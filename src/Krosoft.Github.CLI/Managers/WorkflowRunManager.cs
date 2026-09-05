using Krosoft.Github.CLI.Clients;
using Krosoft.Github.CLI.Helpers;
using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Managers;

internal class WorkflowRunManager : IWorkflowRunManager
{
    public async Task<int> List(string profilePath)
    {
        var (profile, error) = await ProfileLoader.LoadAsync(profilePath);
        if (profile is null)
        {
            return ConsoleHelper.HandleError(error!);
        }

        ConsoleHelper.DisplayHeader($"RUNS EN COURS ET EN ATTENTE - {profile.Name}");

        try
        {
            using IGithubClient client = new GithubClient(profile.Github);

            var repositories = await RepositoryResolver.ResolveAsync(client, profile.Github);
            Console.WriteLine($"Dépôts : {repositories.Count} ({string.Join(", ", repositories)})");
            Console.WriteLine();

            var runs = new List<WorkflowRun>();
            foreach (var repository in repositories)
            {
                runs.AddRange(await client.GetActiveWorkflowRunsAsync(repository));
            }

            if (runs.Count == 0)
            {
                Console.WriteLine("Aucun run en cours ou en attente.");
                return 0;
            }

            Display(runs.OrderBy(r => r.Status, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.CreatedAt).ToList());
            return 0;
        }
        catch (Exception ex)
        {
            return ConsoleHelper.HandleError($"Impossible de lister les runs : {ex.Message}");
        }
    }

    private static void Display(IReadOnlyList<WorkflowRun> runs)
    {
        Console.WriteLine($"{"#",-4} {"ID",-11} {"Dépôt",-24} {"Workflow",-28} {"Statut",-12} {"Branche",-26} {"PR",-7} {"En file depuis",-17} Acteur");
        Console.WriteLine(new string('─', ConsoleHelper.Width));

        var index = 1;
        foreach (var run in runs)
        {
            var repositoryName = run.Repository?.Name ?? "-";
            Console.WriteLine($"{index,-4} {run.Id,-11} {ConsoleHelper.Truncate(repositoryName, 24),-24} " +
                              $"{ConsoleHelper.Truncate(run.Name ?? run.DisplayTitle ?? "-", 28),-28} {run.Status,-12} " +
                              $"{ConsoleHelper.Truncate(run.HeadBranch ?? "-", 26),-26} {run.PullRequestNumber ?? "-",-7} " +
                              $"{run.CreatedAt.LocalDateTime,-17:yyyy-MM-dd HH:mm} {run.Actor?.Login ?? "-"}");
            Console.WriteLine($"     {run.HtmlUrl}");
            index++;
        }

        Console.WriteLine(new string('─', ConsoleHelper.Width));

        var inProgress = runs.Count(r => string.Equals(r.Status, "in_progress", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"Total : {runs.Count} run(s), {inProgress} en cours, {runs.Count - inProgress} en attente");
    }
}
