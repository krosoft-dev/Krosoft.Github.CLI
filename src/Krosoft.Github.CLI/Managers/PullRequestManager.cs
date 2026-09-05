using Krosoft.Github.CLI.Clients;
using Krosoft.Github.CLI.Helpers;
using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Managers;

internal class PullRequestManager : IPullRequestManager
{
    public async Task<int> List(string profilePath)
    {
        var (profile, error) = await ProfileLoader.LoadAsync(profilePath);
        if (profile is null)
        {
            return ConsoleHelper.HandleError(error!);
        }

        ConsoleHelper.DisplayHeader($"PULL REQUESTS - {profile.Name}");

        try
        {
            using IGithubClient client = new GithubClient(profile.Github);

            var (repositories, all, pullRequests) = await FetchAsync(client, profile);
            if (pullRequests.Count == 0)
            {
                Console.WriteLine($"Aucune pull request trouvée ({all.Count} PR(s) analysée(s) dans {repositories.Count} dépôt(s)).");
                return 0;
            }

            Display(pullRequests, all.Count, repositories.Count);
            return 0;
        }
        catch (Exception ex)
        {
            return ConsoleHelper.HandleError($"Impossible de lister les pull requests : {ex.Message}");
        }
    }

    public async Task<int> Approve(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers, bool merge)
    {
        var (profile, error) = await ProfileLoader.LoadAsync(profilePath);
        if (profile is null)
        {
            return ConsoleHelper.HandleError(error!);
        }

        var title = merge ? "APPROBATION + FUSION" : "APPROBATION";
        ConsoleHelper.DisplayHeader(dryRun
                                        ? $"{title} (SIMULATION) - {profile.Name}"
                                        : $"{title} - {profile.Name}");

        try
        {
            using IGithubClient client = new GithubClient(profile.Github);

            var mergeMethod = MergeMethods.Resolve(profile.Github.MergeMethod);
            var login = await client.GetCurrentUserLoginAsync();
            var (repositories, all, pullRequests) = await FetchAsync(client, profile, numbers);

            var missing = numbers.Except(pullRequests.Select(pr => pr.Number)).ToList();
            if (missing.Count > 0)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow,
                                               $"Numéro(s) introuvable(s) parmi les PR au statut '{(profile.PullRequests ?? PullRequestFilters.Default).State.ToString().ToLowerInvariant()}' : {string.Join(", ", missing)}");
                Console.WriteLine();
            }

            if (pullRequests.Count == 0)
            {
                Console.WriteLine($"Aucune pull request trouvée ({all.Count} PR(s) analysée(s) dans {repositories.Count} dépôt(s)).");
                return missing.Count > 0 ? -1 : 0;
            }

            // Détermine l'état de review courant de chaque PR (nécessite un appel par PR sur GitHub).
            var states = new Dictionary<int, string?>();
            foreach (var pr in pullRequests)
            {
                var reviews = await client.GetReviewsAsync(pr);
                states[pr.Number] = ReviewAnalyzer.CurrentStateOf(reviews, login);
            }

            var toApprove = pullRequests.Where(pr => !IsApproved(states, pr)).ToList();
            var alreadyApproved = pullRequests.Count - toApprove.Count;

            Console.WriteLine($"{"#",-4} {"N°",-7} {"Dépôt",-30} {"Titre",-50} {"Mon état",-26} Action");
            Console.WriteLine(new string('─', ConsoleHelper.Width));

            var index = 1;
            foreach (var pr in pullRequests)
            {
                var willApprove = !IsApproved(states, pr);
                var action = willApprove
                    ? dryRun ? "à approuver" : "approbation..."
                    : "ignorée (déjà approuvée)";

                Console.WriteLine($"{index,-4} {pr.Number,-7} {ConsoleHelper.Truncate(pr.RepositoryName, 30),-30} " +
                                  $"{ConsoleHelper.Truncate(pr.Title, 50),-50} {ReviewState.ToLabel(states[pr.Number]),-26} {action}");
                Console.WriteLine($"     {pr.HtmlUrl}");
                index++;
            }

            Console.WriteLine(new string('─', ConsoleHelper.Width));
            Console.WriteLine($"{pullRequests.Count} pull request(s) correspondante(s) sur {all.Count} analysée(s) dans {repositories.Count} dépôt(s) : " +
                              $"{toApprove.Count} à approuver, {alreadyApproved} déjà approuvée(s).");
            Console.WriteLine();

            if (dryRun)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, "Mode simulation : aucune approbation effectuée. Relancer sans --dry-run pour approuver.");
                if (merge)
                {
                    Console.WriteLine();
                    await ExecuteMergeAsync(client, pullRequests, mergeMethod, dryRun: true);
                }

                return 0;
            }

            if (toApprove.Count == 0)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Green, "Rien à approuver.");
            }
            else
            {
                var failures = 0;
                foreach (var pr in toApprove)
                {
                    try
                    {
                        await client.ApproveAsync(pr);
                        ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"  [OK] #{pr.Number} {pr.RepositoryName} approuvée");
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        ConsoleHelper.WriteColoredLine(ConsoleColor.Red, $"  [KO] #{pr.Number} {pr.RepositoryName} : {ex.Message}");
                    }
                }

                Console.WriteLine();
                if (failures > 0)
                {
                    // On n'enchaîne pas la fusion si une approbation a échoué.
                    return ConsoleHelper.HandleError($"{toApprove.Count - failures} approuvée(s), {failures} en échec.");
                }

                ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"{toApprove.Count} pull request(s) approuvée(s).");
            }

            if (merge)
            {
                Console.WriteLine();
                return await ExecuteMergeAsync(client, pullRequests, mergeMethod, dryRun: false);
            }

            return 0;
        }
        catch (Exception ex)
        {
            return ConsoleHelper.HandleError($"Impossible d'approuver les pull requests : {ex.Message}");
        }
    }

    public async Task<int> Merge(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers)
    {
        var (profile, error) = await ProfileLoader.LoadAsync(profilePath);
        if (profile is null)
        {
            return ConsoleHelper.HandleError(error!);
        }

        ConsoleHelper.DisplayHeader(dryRun
                                        ? $"FUSION (SIMULATION) - {profile.Name}"
                                        : $"FUSION - {profile.Name}");

        try
        {
            using IGithubClient client = new GithubClient(profile.Github);

            var mergeMethod = MergeMethods.Resolve(profile.Github.MergeMethod);
            var (repositories, all, pullRequests) = await FetchAsync(client, profile, numbers);

            var missing = numbers.Except(pullRequests.Select(pr => pr.Number)).ToList();
            if (missing.Count > 0)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, $"Numéro(s) introuvable(s) : {string.Join(", ", missing)}");
                Console.WriteLine();
            }

            if (pullRequests.Count == 0)
            {
                Console.WriteLine($"Aucune pull request trouvée ({all.Count} PR(s) analysée(s) dans {repositories.Count} dépôt(s)).");
                return missing.Count > 0 ? -1 : 0;
            }

            return await ExecuteMergeAsync(client, pullRequests, mergeMethod, dryRun);
        }
        catch (Exception ex)
        {
            return ConsoleHelper.HandleError($"Impossible de fusionner les pull requests : {ex.Message}");
        }
    }

    // Affiche le plan de fusion puis exécute (sauf en simulation). Partagé par la commande pr-merge et l'option --merge de pr-approve.
    private static async Task<int> ExecuteMergeAsync(IGithubClient client, IReadOnlyList<PullRequest> pullRequests, string mergeMethod, bool dryRun)
    {
        // Les PR en brouillon ne sont jamais fusionnables : on les écarte du plan.
        var toMerge = pullRequests.Where(pr => !pr.IsDraft).ToList();
        var drafts = pullRequests.Count - toMerge.Count;

        Console.WriteLine($"Méthode de fusion : {mergeMethod}");
        Console.WriteLine();
        Console.WriteLine($"{"#",-4} {"N°",-7} {"Dépôt",-30} {"Titre",-60} Action");
        Console.WriteLine(new string('─', ConsoleHelper.Width));

        var index = 1;
        foreach (var pr in pullRequests)
        {
            var action = pr.IsDraft
                ? "ignorée (brouillon)"
                : dryRun ? "à fusionner" : "fusion...";

            Console.WriteLine($"{index,-4} {pr.Number,-7} {ConsoleHelper.Truncate(pr.RepositoryName, 30),-30} " +
                              $"{ConsoleHelper.Truncate(pr.Title, 60),-60} {action}");
            Console.WriteLine($"     {pr.HtmlUrl}");
            index++;
        }

        Console.WriteLine(new string('─', ConsoleHelper.Width));
        Console.WriteLine($"{toMerge.Count} à fusionner, {drafts} brouillon(s) ignoré(s).");
        Console.WriteLine();

        if (dryRun)
        {
            ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, "Mode simulation : aucune fusion effectuée. Relancer sans --dry-run pour fusionner.");
            return 0;
        }

        if (toMerge.Count == 0)
        {
            ConsoleHelper.WriteColoredLine(ConsoleColor.Green, "Rien à fusionner.");
            return 0;
        }

        var failures = 0;
        foreach (var pr in toMerge)
        {
            try
            {
                await client.MergeAsync(pr, mergeMethod);
                ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"  [OK] #{pr.Number} {pr.RepositoryName} fusionnée");
            }
            catch (Exception ex)
            {
                failures++;
                ConsoleHelper.WriteColoredLine(ConsoleColor.Red, $"  [KO] #{pr.Number} {pr.RepositoryName} : {ex.Message}");
            }
        }

        Console.WriteLine();
        if (failures > 0)
        {
            return ConsoleHelper.HandleError($"{toMerge.Count - failures} fusionnée(s), {failures} en échec.");
        }

        ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"{toMerge.Count} pull request(s) fusionnée(s).");
        return 0;
    }

    public async Task<int> Rerun(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers)
    {
        var (profile, error) = await ProfileLoader.LoadAsync(profilePath);
        if (profile is null)
        {
            return ConsoleHelper.HandleError(error!);
        }

        ConsoleHelper.DisplayHeader(dryRun
                                        ? $"RELANCE DES RUNS (SIMULATION) - {profile.Name}"
                                        : $"RELANCE DES RUNS - {profile.Name}");

        try
        {
            using IGithubClient client = new GithubClient(profile.Github);

            var (repositories, all, pullRequests) = await FetchAsync(client, profile, numbers);

            var missing = numbers.Except(pullRequests.Select(pr => pr.Number)).ToList();
            if (missing.Count > 0)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, $"Numéro(s) introuvable(s) : {string.Join(", ", missing)}");
                Console.WriteLine();
            }

            if (pullRequests.Count == 0)
            {
                Console.WriteLine($"Aucune pull request trouvée ({all.Count} PR(s) analysée(s) dans {repositories.Count} dépôt(s)).");
                return missing.Count > 0 ? -1 : 0;
            }

            // Inspection des runs GitHub Actions du dernier commit de chaque PR : on ne relance que les runs en échec.
            var plan = new List<(PullRequest pr, WorkflowRun run)>();
            var index = 1;
            foreach (var pr in pullRequests)
            {
                Console.WriteLine($"{index,-4} #{pr.Number,-7} {pr.RepositoryName}  {ConsoleHelper.Truncate(pr.Title, 60)}");
                Console.WriteLine($"     {pr.HtmlUrl}");

                var runs = LatestRunPerWorkflow(await client.GetWorkflowRunsForCommitAsync(pr));
                var failed = runs.Where(r => r.CanRerun).ToList();

                if (failed.Count == 0)
                {
                    ConsoleHelper.WriteColoredLine(ConsoleColor.DarkGray, "     aucun run en échec");
                }

                foreach (var run in failed)
                {
                    plan.Add((pr, run));
                    ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, $"     [{run.Conclusion}] {run.Name ?? run.DisplayTitle} -> {(dryRun ? "à relancer" : "relance")}");
                }

                index++;
            }

            Console.WriteLine(new string('─', ConsoleHelper.Width));
            Console.WriteLine($"{pullRequests.Count} pull request(s) inspectée(s) sur {all.Count} analysée(s) dans {repositories.Count} dépôt(s) : {plan.Count} run(s) à relancer.");
            Console.WriteLine();

            if (dryRun)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, "Mode simulation : aucun run relancé. Relancer sans --dry-run pour exécuter.");
                return 0;
            }

            if (plan.Count == 0)
            {
                ConsoleHelper.WriteColoredLine(ConsoleColor.Green, "Rien à relancer.");
                return 0;
            }

            var failures = 0;
            foreach (var (pr, run) in plan)
            {
                try
                {
                    await client.RerunFailedJobsAsync(pr, run.Id);
                    ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"  [OK] #{pr.Number} {pr.RepositoryName} : {run.Name ?? run.DisplayTitle} relancé");
                }
                catch (Exception ex)
                {
                    failures++;
                    ConsoleHelper.WriteColoredLine(ConsoleColor.Red, $"  [KO] #{pr.Number} {pr.RepositoryName} : {run.Name ?? run.DisplayTitle} : {ex.Message}");
                }
            }

            Console.WriteLine();
            if (failures > 0)
            {
                return ConsoleHelper.HandleError($"{plan.Count - failures} relancé(s), {failures} en échec.");
            }

            ConsoleHelper.WriteColoredLine(ConsoleColor.Green, $"{plan.Count} run(s) relancé(s).");
            return 0;
        }
        catch (Exception ex)
        {
            return ConsoleHelper.HandleError($"Impossible de relancer les runs : {ex.Message}");
        }
    }

    private static bool IsApproved(IReadOnlyDictionary<int, string?> states, PullRequest pr) =>
        string.Equals(states[pr.Number], ReviewState.Approved, StringComparison.OrdinalIgnoreCase);

    // Un commit peut avoir plusieurs runs d'un même workflow (relances) : on ne garde que le plus récent de chaque workflow.
    private static IReadOnlyList<WorkflowRun> LatestRunPerWorkflow(IEnumerable<WorkflowRun> runs) =>
        runs.GroupBy(r => r.WorkflowId)
            .Select(g => g.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.RunNumber).First())
            .ToList();

    // Résout les dépôts, récupère toutes les PR de l'état demandé, puis applique les filtres du profil
    // (ou la sélection explicite par numéro si elle est fournie).
    private static async Task<(IReadOnlyList<string> repositories, IReadOnlyList<PullRequest> all, IReadOnlyList<PullRequest> filtered)> FetchAsync(
        IGithubClient client,
        Profile profile,
        IReadOnlyCollection<int>? numbers = null)
    {
        var filters = profile.PullRequests ?? PullRequestFilters.Default;
        var byNumbers = numbers is { Count: > 0 };

        var repositories = await RepositoryResolver.ResolveAsync(client, profile.Github);
        DisplayFilters(repositories, filters, byNumbers ? numbers : null);

        var all = new List<PullRequest>();
        foreach (var repository in repositories)
        {
            all.AddRange(await client.GetPullRequestsAsync(repository, filters.State));
        }

        var filtered = byNumbers
            ? PullRequestFilter.ByNumbers(all, numbers!)
            : PullRequestFilter.Apply(all, filters);

        return (repositories, all, filtered);
    }

    private static void DisplayFilters(IReadOnlyList<string> repositories, PullRequestFilters filters, IReadOnlyCollection<int>? numbers)
    {
        Console.WriteLine($"Dépôts : {repositories.Count} ({string.Join(", ", repositories)})");
        Console.WriteLine($"État   : {filters.State.ToString().ToLowerInvariant()}");

        if (numbers is { Count: > 0 })
        {
            Console.WriteLine($"N°     : {string.Join(", ", numbers)} (les filtres titre/dépôt du profil sont ignorés)");
            Console.WriteLine();
            return;
        }

        if (filters.Titles is { Count: > 0 })
        {
            Console.WriteLine($"Titre  : {(filters.ExactTitle ? "égal à" : "contient")} {string.Join(" | ", filters.Titles.Select(t => $"\"{t}\""))}");
        }

        if (filters.Repositories is { Count: > 0 })
        {
            Console.WriteLine($"Filtre dépôts : {string.Join(", ", filters.Repositories)}");
        }

        Console.WriteLine();
    }

    private static void Display(IReadOnlyList<PullRequest> pullRequests, int totalAnalyzed, int repositoryCount)
    {
        Console.WriteLine($"{"#",-4} {"N°",-7} {"Dépôt",-30} {"Titre",-50} {"Auteur",-20} {"Créée le",-17} État");
        Console.WriteLine(new string('─', ConsoleHelper.Width));

        var index = 1;
        foreach (var pr in pullRequests)
        {
            var draft = pr.IsDraft ? " (draft)" : string.Empty;
            Console.WriteLine($"{index,-4} {pr.Number,-7} {ConsoleHelper.Truncate(pr.RepositoryName, 30),-30} " +
                              $"{ConsoleHelper.Truncate(pr.Title, 50),-50} {ConsoleHelper.Truncate(pr.User.Login, 20),-20} " +
                              $"{pr.CreatedAt.LocalDateTime,-17:yyyy-MM-dd HH:mm} {pr.State}{draft}");
            Console.WriteLine($"     {pr.HtmlUrl}");
            index++;
        }

        Console.WriteLine(new string('─', ConsoleHelper.Width));
        Console.WriteLine($"Total : {pullRequests.Count} pull request(s) sur {totalAnalyzed} analysée(s) dans {repositoryCount} dépôt(s)");
    }
}
