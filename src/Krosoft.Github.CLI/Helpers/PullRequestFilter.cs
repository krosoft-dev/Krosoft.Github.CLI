using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Helpers;

internal static class PullRequestFilter
{
    internal static IReadOnlyList<PullRequest> Apply(IEnumerable<PullRequest> pullRequests, PullRequestFilters filters)
    {
        var query = pullRequests;

        var titles = Clean(filters.Titles);
        if (titles.Count > 0)
        {
            query = filters.ExactTitle
                ? query.Where(pr => titles.Any(t => string.Equals(pr.Title.Trim(), t, StringComparison.OrdinalIgnoreCase)))
                : query.Where(pr => titles.Any(t => pr.Title.Contains(t, StringComparison.OrdinalIgnoreCase)));
        }

        var repositories = Clean(filters.Repositories);
        if (repositories.Count > 0)
        {
            query = query.Where(pr => repositories.Contains(pr.RepositoryName, StringComparer.OrdinalIgnoreCase));
        }

        return query.OrderBy(pr => pr.RepositoryName, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(pr => pr.CreatedAt)
                    .ToList();
    }

    // Sélection explicite par numéro de PR. Attention : un numéro n'est unique qu'au sein d'un dépôt,
    // donc un même numéro peut correspondre à plusieurs PR si le scan couvre plusieurs dépôts.
    internal static IReadOnlyList<PullRequest> ByNumbers(IEnumerable<PullRequest> pullRequests, IReadOnlyCollection<int> numbers)
    {
        var wanted = numbers.ToHashSet();
        return pullRequests.Where(pr => wanted.Contains(pr.Number))
                           .OrderBy(pr => pr.RepositoryName, StringComparer.OrdinalIgnoreCase)
                           .ThenByDescending(pr => pr.CreatedAt)
                           .ToList();
    }

    private static List<string> Clean(IEnumerable<string>? values) =>
        values is null
            ? []
            : values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList();
}
