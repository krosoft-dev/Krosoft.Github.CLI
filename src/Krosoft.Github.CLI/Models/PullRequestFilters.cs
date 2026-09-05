using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record PullRequestFilters(
    [property: JsonPropertyName("state")] PullRequestState State = PullRequestState.Open,
    [property: JsonPropertyName("titles")] List<string>? Titles = null,
    [property: JsonPropertyName("exactTitle")]
    bool ExactTitle = false,
    [property: JsonPropertyName("repositories")]
    List<string>? Repositories = null)
{
    internal static readonly PullRequestFilters Default = new();
}
