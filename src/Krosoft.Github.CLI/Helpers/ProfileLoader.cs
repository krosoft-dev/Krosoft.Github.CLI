using System.Text.Json;
using System.Text.Json.Serialization;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Helpers;

internal static class ProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static async Task<(Profile? profile, string? error)> LoadAsync(string path)
    {
        if (!File.Exists(path))
        {
            return (null, $"Profil introuvable : {path}");
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var profile = JsonSerializer.Deserialize<Profile>(json, Options);
            if (profile is null)
            {
                return (null, "Profil invalide ou vide.");
            }

            var validationError = Validate(profile);
            return validationError is null
                ? (profile, null)
                : (null, validationError);
        }
        catch (Exception ex)
        {
            return (null, $"Erreur de lecture du profil : {ex.Message}");
        }
    }

    private static string? Validate(Profile profile)
    {
        if (profile.Github is null)
        {
            return "Profil invalide : la section 'github' est requise.";
        }

        if (string.IsNullOrWhiteSpace(profile.Github.Owner))
        {
            return "Profil invalide : le champ 'github.owner' est requis (organisation ou utilisateur, ex: krosoft-dev).";
        }

        if (string.IsNullOrWhiteSpace(profile.Github.Pat))
        {
            return "Profil invalide : le champ 'github.pat' (Personal Access Token) est requis.";
        }

        if (!string.IsNullOrWhiteSpace(profile.Github.ApiUrl) &&
            !Uri.TryCreate(profile.Github.ApiUrl, UriKind.Absolute, out _))
        {
            return $"Profil invalide : 'github.apiUrl' n'est pas une URL valide ({profile.Github.ApiUrl}).";
        }

        if (!string.IsNullOrWhiteSpace(profile.Github.MergeMethod) &&
            !MergeMethods.IsValid(profile.Github.MergeMethod))
        {
            return $"Profil invalide : 'github.mergeMethod' doit valoir {string.Join(", ", MergeMethods.Allowed)} (reçu : {profile.Github.MergeMethod}).";
        }

        return null;
    }
}
