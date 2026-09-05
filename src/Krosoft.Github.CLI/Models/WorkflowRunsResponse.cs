using System.Text.Json.Serialization;

namespace Krosoft.Github.CLI.Models;

// L'API Actions encapsule les runs dans un objet (contrairement aux endpoints pulls/reviews/repos qui renvoient un tableau).
internal record WorkflowRunsResponse(
    [property: JsonPropertyName("total_count")]
    int TotalCount,
    [property: JsonPropertyName("workflow_runs")]
    List<WorkflowRun> WorkflowRuns);
