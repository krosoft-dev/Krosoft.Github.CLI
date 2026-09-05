using Krosoft.Github.CLI.Helpers;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Tests.Helpers;

[TestClass]
public class ReviewAnalyzerTests
{
    private const string Me = "kevin";
    private const string Other = "renovate[bot]";

    [TestMethod]
    public void IsApprovedBy_SansReview_Faux()
    {
        var reviews = new List<Review>();

        Check.That(ReviewAnalyzer.IsApprovedBy(reviews, Me)).IsFalse();
        Check.That(ReviewAnalyzer.CurrentStateOf(reviews, Me)).IsNull();
    }

    [TestMethod]
    public void IsApprovedBy_ApprouveParUnAutre_Faux()
    {
        var reviews = new List<Review> { Review(Other, ReviewState.Approved, 1) };

        Check.That(ReviewAnalyzer.IsApprovedBy(reviews, Me)).IsFalse();
    }

    [TestMethod]
    public void IsApprovedBy_ApprouveParMoi_InsensibleALaCasse_Vrai()
    {
        var reviews = new List<Review>
        {
            Review(Other, ReviewState.ChangesRequested, 1),
            Review("KEVIN", ReviewState.Approved, 2)
        };

        Check.That(ReviewAnalyzer.IsApprovedBy(reviews, Me)).IsTrue();
        Check.That(ReviewAnalyzer.CurrentStateOf(reviews, Me)).IsEqualTo(ReviewState.Approved);
    }

    [TestMethod]
    public void CurrentStateOf_DerniereReviewDecisiveGagne()
    {
        // J'approuve puis je demande des modifications : l'état courant est « modifications demandées ».
        var reviews = new List<Review>
        {
            Review(Me, ReviewState.Approved, 1, new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
            Review(Me, ReviewState.ChangesRequested, 2, new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero))
        };

        Check.That(ReviewAnalyzer.CurrentStateOf(reviews, Me)).IsEqualTo(ReviewState.ChangesRequested);
        Check.That(ReviewAnalyzer.IsApprovedBy(reviews, Me)).IsFalse();
    }

    [TestMethod]
    public void CurrentStateOf_LesCommentairesNeComptentPas()
    {
        // Une review COMMENTED postée après l'approbation ne change pas l'état décisif.
        var reviews = new List<Review>
        {
            Review(Me, ReviewState.Approved, 1, new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
            Review(Me, ReviewState.Commented, 2, new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero))
        };

        Check.That(ReviewAnalyzer.CurrentStateOf(reviews, Me)).IsEqualTo(ReviewState.Approved);
        Check.That(ReviewAnalyzer.IsApprovedBy(reviews, Me)).IsTrue();
    }

    private static Review Review(string login, string state, long id, DateTimeOffset? submittedAt = null) =>
        new(id, new User(login, id), state, submittedAt);
}
