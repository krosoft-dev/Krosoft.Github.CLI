using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record GithubProfile(
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("pat")] string Pat,
    [property: JsonPropertyName("repositories")]
    List<string>? Repositories = null,
    [property: JsonPropertyName("apiUrl")] string? ApiUrl = null,
    [property: JsonPropertyName("mergeMethod")]
    string? MergeMethod = null);
