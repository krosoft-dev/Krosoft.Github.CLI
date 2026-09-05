using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Tests.Models;

[TestClass]
public class MergeMethodsTests
{
    [TestMethod]
    public void Resolve_ValeurNulle_RetourneDefautSquash()
    {
        Check.That(MergeMethods.Resolve(null)).IsEqualTo("squash");
        Check.That(MergeMethods.Default).IsEqualTo("squash");
    }

    [TestMethod]
    public void Resolve_ValeurValide_InsensibleALaCasse_Normalisee()
    {
        Check.That(MergeMethods.Resolve("MERGE")).IsEqualTo("merge");
        Check.That(MergeMethods.Resolve("Rebase")).IsEqualTo("rebase");
    }

    [TestMethod]
    public void Resolve_ValeurInconnue_RetourneDefaut()
    {
        Check.That(MergeMethods.Resolve("fast-forward")).IsEqualTo("squash");
    }

    [TestMethod]
    public void IsValid()
    {
        Check.That(MergeMethods.IsValid("squash")).IsTrue();
        Check.That(MergeMethods.IsValid("merge")).IsTrue();
        Check.That(MergeMethods.IsValid("rebase")).IsTrue();
        Check.That(MergeMethods.IsValid("autre")).IsFalse();
        Check.That(MergeMethods.IsValid(null)).IsFalse();
    }
}
