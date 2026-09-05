namespace Krosoft.Github.CLI.Models;

// États possibles d'une review GitHub (champ "state").
internal static class ReviewState
{
    internal const string Approved = "APPROVED";
    internal const string ChangesRequested = "CHANGES_REQUESTED";
    internal const string Commented = "COMMENTED";
    internal const string Dismissed = "DISMISSED";
    internal const string Pending = "PENDING";

    internal static string ToLabel(string? state) => state switch
    {
        Approved => "approuvée",
        ChangesRequested => "modifications demandées",
        Commented => "commentée",
        Dismissed => "rejetée (annulée)",
        Pending => "en attente",
        null or "" => "sans review",
        _ => state.ToLowerInvariant()
    };
}
