using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

// Exécution GitHub Actions.
// status : queued, in_progress, completed. conclusion (si completed) : success, failure, cancelled, timed_out, startup_failure, ...
internal record WorkflowRun(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("display_title")]
    string? DisplayTitle,
    [property: JsonPropertyName("run_number")]
    int RunNumber,
    [property: JsonPropertyName("workflow_id")]
    long WorkflowId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("conclusion")]
    string? Conclusion,
    [property: JsonPropertyName("event")] string Event,
    [property: JsonPropertyName("head_branch")]
    string? HeadBranch,
    [property: JsonPropertyName("head_sha")]
    string HeadSha,
    [property: JsonPropertyName("created_at")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("html_url")]
    string HtmlUrl,
    [property: JsonPropertyName("actor")] User? Actor,
    [property: JsonPropertyName("repository")]
    Repository? Repository = null,
    [property: JsonPropertyName("pull_requests")]
    List<PullRequestRef>? PullRequests = null)
{
    // Numéro de la PR à l'origine du run, si le run a été déclenché par une pull request.
    internal string? PullRequestNumber =>
        PullRequests is { Count: > 0 } prs ? prs[0].Number.ToString() : null;

    internal bool IsCompleted => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);

    // Conclusions considérées comme un échec relançable (équivalent du bouton "Re-run failed jobs").
    internal bool IsFailed =>
        string.Equals(Conclusion, "failure", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Conclusion, "timed_out", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Conclusion, "startup_failure", StringComparison.OrdinalIgnoreCase);

    internal bool CanRerun => IsCompleted && IsFailed;
}
