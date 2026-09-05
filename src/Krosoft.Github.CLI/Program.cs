using CommandLine;

namespace Krosoft.Github.CLI;

internal static class Program
{
    private static async Task<int> Main(params string[] args)
    {
        PrintBanner();
        using var parser = new Parser(settings =>
        {
            settings.HelpWriter = Console.Error;
            settings.CaseInsensitiveEnumValues = true;
        });
        return await parser.ParseArguments<Options.PullRequestsOptions, Options.ApproveOptions, Options.MergeOptions, Options.RerunOptions, Options.RunListOptions>(args)
                           .MapResult(
                                      (Options.PullRequestsOptions opts) => ProgramPullRequests.List(opts),
                                      (Options.ApproveOptions opts) => ProgramPullRequests.Approve(opts),
                                      (Options.MergeOptions opts) => ProgramPullRequests.Merge(opts),
                                      (Options.RerunOptions opts) => ProgramPullRequests.Rerun(opts),
                                      (Options.RunListOptions opts) => ProgramWorkflowRuns.List(opts),
                                      _ => Task.FromResult(-1));
    }

    private static void PrintBanner()
    {
        const string banner = """

                                _  __                     __ _
                               | |/ /                    / _| |
                               | ' / _ __ ___  ___  ___ | |_| |_
                               |  < | '__/ _ \/ __|/ _ \|  _| __|
                               | . \| | | (_) \__ \ (_) | | | |_
                               |_|\_\_|  \___/|___/\___/|_|  \__|

                               GitHub CLI Tool

                              """;
        Console.WriteLine(banner);
    }
}
