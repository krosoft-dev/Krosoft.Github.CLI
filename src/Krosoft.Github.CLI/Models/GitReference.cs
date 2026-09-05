using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

// Extrémité d'une pull request (head ou base) : branche, commit et dépôt associés.
internal record GitReference(
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("sha")] string Sha,
    [property: JsonPropertyName("repo")] Repository? Repo = null);
