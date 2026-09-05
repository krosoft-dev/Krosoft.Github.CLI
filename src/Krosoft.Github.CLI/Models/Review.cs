using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record Review(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("user")] User? User,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("submitted_at")]
    DateTimeOffset? SubmittedAt = null);
