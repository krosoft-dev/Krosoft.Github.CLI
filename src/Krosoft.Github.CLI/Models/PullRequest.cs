using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record PullRequest(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("draft")] bool IsDraft,
    [property: JsonPropertyName("created_at")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("html_url")]
    string HtmlUrl,
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("head")] GitReference Head,
    [property: JsonPropertyName("base")] GitReference Base)
{
    // Dépôt propriétaire de la pull request (la base), utilisé pour les appels API et l'affichage.
    internal string RepositoryName => Base.Repo?.Name ?? string.Empty;
}
