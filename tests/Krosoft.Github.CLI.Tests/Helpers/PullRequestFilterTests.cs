using Krosoft.Github.CLI.Helpers;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Tests.Helpers;

[TestClass]
public class PullRequestFilterTests
{
    private static readonly List<PullRequest> PullRequests =
    [
        Create(1, "Renovate - Update all Krosoft.Extensions packages", "Repo.B", new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
        Create(2, "Renovate - Update all Krosoft.Extensions packages", "Repo.A", new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero)),
        Create(3, "renovate - update all krosoft.extensions packages (major)", "Repo.A", new DateTimeOffset(2026, 8, 30, 8, 0, 0, TimeSpan.Zero)),
        Create(4, "Feature/login", "Repo.C", new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero))
    ];

    [TestMethod]
    public void Apply_SansFiltre_RetourneToutTrieParDepotPuisDate()
    {
        var result = PullRequestFilter.Apply(PullRequests, PullRequestFilters.Default);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(2, 3, 1, 4);
    }

    [TestMethod]
    public void Apply_TitreContient_InsensibleALaCasse()
    {
        var filters = new PullRequestFilters(Titles: ["krosoft.extensions"]);

        var result = PullRequestFilter.Apply(PullRequests, filters);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(2, 3, 1);
    }

    [TestMethod]
    public void Apply_PlusieursTitres_CombineEnOu()
    {
        var filters = new PullRequestFilters(Titles: ["(major)", "Feature/"]);

        var result = PullRequestFilter.Apply(PullRequests, filters);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(3, 4);
    }

    [TestMethod]
    public void Apply_TitreExact_ExclutLesVariantes()
    {
        var filters = new PullRequestFilters(Titles: ["Renovate - Update all Krosoft.Extensions packages"], ExactTitle: true);

        var result = PullRequestFilter.Apply(PullRequests, filters);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(2, 1);
    }

    [TestMethod]
    public void Apply_Depots_LimiteAuxDepotsDemandes()
    {
        var filters = new PullRequestFilters(Titles: ["Renovate"], Repositories: ["repo.a", "Repo.C"]);

        var result = PullRequestFilter.Apply(PullRequests, filters);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(2, 3);
    }

    [TestMethod]
    public void Apply_ValeursVides_SontIgnorees()
    {
        var filters = new PullRequestFilters(Titles: ["", "  "], Repositories: [" "]);

        var result = PullRequestFilter.Apply(PullRequests, filters);

        Check.That(result).HasSize(4);
    }

    [TestMethod]
    public void ByNumbers_RetourneUniquementLesNumerosDemandes_IgnoreLesInconnus()
    {
        var result = PullRequestFilter.ByNumbers(PullRequests, [4, 1, 999]);

        Check.That(result.Select(pr => pr.Number)).ContainsExactly(1, 4);
    }

    [TestMethod]
    public void ByNumbers_ListeVide_NeRetourneRien()
    {
        var result = PullRequestFilter.ByNumbers(PullRequests, []);

        Check.That(result).IsEmpty();
    }

    private static PullRequest Create(int number, string title, string repository, DateTimeOffset createdAt) =>
        new(number,
            title,
            "open",
            false,
            createdAt,
            $"https://github.com/krosoft-dev/{repository}/pull/{number}",
            new User("renovate[bot]", 1),
            new GitReference("renovate/all", "abc123", new Repository(repository)),
            new GitReference("main", "def456", new Repository(repository)));
}
