using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Managers;

namespace Krosoft.Github.CLI;

internal static class ProgramWorkflowRuns
{
    public static Task<int> List(Options.RunListOptions opts) => GetWorkflowRunManager().List(opts.Profile);

    private static IWorkflowRunManager GetWorkflowRunManager() => new WorkflowRunManager();
}
