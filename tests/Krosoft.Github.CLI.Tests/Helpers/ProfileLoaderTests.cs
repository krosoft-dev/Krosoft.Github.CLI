using Krosoft.Github.CLI.Helpers;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Tests.Helpers;

[TestClass]
public class ProfileLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_ProfilComplet_LitLesFiltres()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev", "pat": "secret", "repositories": ["Repo.A"] },
              "pullRequests": {
                "state": "closed",
                "titles": ["Renovate - Update all Krosoft.Extensions packages"],
                "exactTitle": true,
                "repositories": ["Repo.A"]
              }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(error).IsNull();
        Check.That(profile).IsNotNull();
        Check.That(profile!.PullRequests).IsNotNull();
        Check.That(profile.PullRequests!.State).IsEqualTo(PullRequestState.Closed);
        Check.That(profile.PullRequests.Titles).ContainsExactly("Renovate - Update all Krosoft.Extensions packages");
        Check.That(profile.PullRequests.ExactTitle).IsTrue();
        Check.That(profile.PullRequests.Repositories).ContainsExactly("Repo.A");
    }

    [TestMethod]
    public async Task LoadAsync_SansSectionPullRequests_ProfilValide()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev", "pat": "secret", "repositories": ["Repo"] }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(error).IsNull();
        Check.That(profile!.PullRequests).IsNull();
    }

    [TestMethod]
    public async Task LoadAsync_EtatInconnu_RetourneUneErreur()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev", "pat": "secret", "repositories": ["Repo"] },
              "pullRequests": { "state": "merged" }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(profile).IsNull();
        Check.That(error).StartsWith("Erreur de lecture du profil");
    }

    [TestMethod]
    public async Task LoadAsync_PatManquant_RetourneUneErreur()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev" }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(profile).IsNull();
        Check.That(error).Contains("github.pat");
    }

    [TestMethod]
    public async Task LoadAsync_OwnerManquant_RetourneUneErreur()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "pat": "secret" }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(profile).IsNull();
        Check.That(error).Contains("github.owner");
    }

    [TestMethod]
    public async Task LoadAsync_DepotsAbsents_ProfilValide()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev", "pat": "secret" }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(error).IsNull();
        Check.That(profile!.Github.Repositories).IsNull();
    }

    [TestMethod]
    public async Task LoadAsync_ApiUrlInvalide_RetourneUneErreur()
    {
        var path = await WriteTempProfile("""
            {
              "name": "test",
              "github": { "owner": "krosoft-dev", "pat": "secret", "apiUrl": "pas-une-url" }
            }
            """);

        var (profile, error) = await ProfileLoader.LoadAsync(path);

        Check.That(profile).IsNull();
        Check.That(error).Contains("github.apiUrl");
    }

    private static async Task<string> WriteTempProfile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"krosoft-github-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
