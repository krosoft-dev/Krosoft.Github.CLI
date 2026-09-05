using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record Repository(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("full_name")]
    string? FullName = null,
    [property: JsonPropertyName("owner")] User? Owner = null,
    [property: JsonPropertyName("archived")]
    bool Archived = false,
    [property: JsonPropertyName("fork")] bool Fork = false);
