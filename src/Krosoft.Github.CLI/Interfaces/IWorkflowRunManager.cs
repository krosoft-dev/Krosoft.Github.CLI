namespace Krosoft.Github.CLI.Interfaces;

internal interface IWorkflowRunManager
{
    Task<int> List(string profilePath);
}
