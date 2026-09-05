using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

// Référence légère vers une pull request, telle qu'exposée dans un workflow run.
internal record PullRequestRef(
    [property: JsonPropertyName("number")] int Number);
