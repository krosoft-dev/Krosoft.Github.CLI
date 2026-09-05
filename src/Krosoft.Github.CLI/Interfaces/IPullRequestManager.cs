namespace Krosoft.Github.CLI.Interfaces;

internal interface IPullRequestManager
{
    Task<int> List(string profilePath);

    Task<int> Approve(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers, bool merge);

    Task<int> Merge(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers);

    Task<int> Rerun(string profilePath, bool dryRun, IReadOnlyCollection<int> numbers);
}
