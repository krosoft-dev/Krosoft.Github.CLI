using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Helpers;

internal static class RepositoryResolver
{
    // Dépôts explicitement listés dans le profil, sinon tous les dépôts (non archivés) de l'owner.
    internal static async Task<IReadOnlyList<string>> ResolveAsync(IGithubClient client, GithubProfile profile)
    {
        var configured = (profile.Repositories ?? [])
                         .Where(r => !string.IsNullOrWhiteSpace(r))
                         .Select(r => r.Trim())
                         .ToList();

        if (configured.Count > 0)
        {
            return configured;
        }

        var repositories = await client.GetRepositoriesAsync();
        return repositories.Select(r => r.Name).ToList();
    }
}
