using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Tests.Models;

[TestClass]
public class WorkflowRunTests
{
    [TestMethod]
    public void CanRerun_RunEnEchec_Vrai()
    {
        var run = Create("completed", "failure");

        Check.That(run.IsCompleted).IsTrue();
        Check.That(run.IsFailed).IsTrue();
        Check.That(run.CanRerun).IsTrue();
    }

    [TestMethod]
    public void CanRerun_RunTimedOut_Vrai()
    {
        Check.That(Create("completed", "timed_out").CanRerun).IsTrue();
        Check.That(Create("completed", "startup_failure").CanRerun).IsTrue();
    }

    [TestMethod]
    public void CanRerun_RunReussi_Faux()
    {
        var run = Create("completed", "success");

        Check.That(run.IsFailed).IsFalse();
        Check.That(run.CanRerun).IsFalse();
    }

    [TestMethod]
    public void CanRerun_RunEnCours_Faux()
    {
        var run = Create("in_progress", null);

        Check.That(run.IsCompleted).IsFalse();
        Check.That(run.CanRerun).IsFalse();
    }

    [TestMethod]
    public void CanRerun_RunAnnule_Faux()
    {
        // Une annulation n'est pas relançable via « Re-run failed jobs ».
        var run = Create("completed", "cancelled");

        Check.That(run.CanRerun).IsFalse();
    }

    [TestMethod]
    public void PullRequestNumber_DepuisPullRequests()
    {
        Check.That(Create("in_progress", null, [new PullRequestRef(6562)]).PullRequestNumber).IsEqualTo("6562");
        Check.That(Create("in_progress", null, []).PullRequestNumber).IsNull();
        Check.That(Create("in_progress", null).PullRequestNumber).IsNull();
    }

    private static WorkflowRun Create(string status, string? conclusion, List<PullRequestRef>? pullRequests = null) =>
        new(1,
            "Build",
            "Renovate - Update all Krosoft.Extensions packages",
            42,
            100,
            status,
            conclusion,
            "pull_request",
            "renovate/all",
            "abc123",
            DateTimeOffset.UtcNow,
            "https://github.com/krosoft-dev/Repo/actions/runs/1",
            new User("renovate[bot]", 1),
            new Repository("Repo"),
            pullRequests);
}
