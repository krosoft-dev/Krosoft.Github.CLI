using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Interfaces;

internal interface IGithubClient : IDisposable
{
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string repository, PullRequestState state, CancellationToken cancellationToken = default);

    Task<string> GetCurrentUserLoginAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> GetReviewsAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);

    Task ApproveAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);

    Task MergeAsync(PullRequest pullRequest, string mergeMethod, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> GetWorkflowRunsForCommitAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);

    Task RerunFailedJobsAsync(PullRequest pullRequest, long runId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> GetActiveWorkflowRunsAsync(string repository, CancellationToken cancellationToken = default);
}
