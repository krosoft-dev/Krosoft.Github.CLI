using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record Profile(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("github")] GithubProfile Github,
    [property: JsonPropertyName("pullRequests")]
    PullRequestFilters? PullRequests = null);
