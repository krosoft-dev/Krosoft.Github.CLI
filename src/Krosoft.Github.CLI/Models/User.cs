using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

internal record User(
    [property: JsonPropertyName("login")] string Login,
    [property: JsonPropertyName("id")] long Id);
