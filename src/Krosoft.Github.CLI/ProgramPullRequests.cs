using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Managers;

namespace Krosoft.Github.CLI;

internal static class ProgramPullRequests
{
    public static Task<int> List(Options.PullRequestsOptions opts) => GetPullRequestManager().List(opts.Profile);

    public static Task<int> Approve(Options.ApproveOptions opts) =>
        GetPullRequestManager().Approve(opts.Profile, opts.DryRun, opts.Numbers.ToList(), opts.Merge);

    public static Task<int> Merge(Options.MergeOptions opts) =>
        GetPullRequestManager().Merge(opts.Profile, opts.DryRun, opts.Numbers.ToList());

    public static Task<int> Rerun(Options.RerunOptions opts) =>
        GetPullRequestManager().Rerun(opts.Profile, opts.DryRun, opts.Numbers.ToList());

    private static IPullRequestManager GetPullRequestManager() => new PullRequestManager();
}
