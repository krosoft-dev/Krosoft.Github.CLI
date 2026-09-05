using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Helpers;

internal static class ReviewAnalyzer
{
    // État de review courant d'un utilisateur : GitHub conserve l'historique complet, seule la dernière
    // review « décisive » (approbation, demande de modifications ou annulation) compte. Les reviews
    // simplement commentées (COMMENTED) ou en attente (PENDING) ne changent pas l'état.
    internal static string? CurrentStateOf(IEnumerable<Review> reviews, string login)
    {
        var decisive = new[] { ReviewState.Approved, ReviewState.ChangesRequested, ReviewState.Dismissed };

        return reviews
               .Where(r => r.User is not null && string.Equals(r.User.Login, login, StringComparison.OrdinalIgnoreCase))
               .Where(r => decisive.Contains(r.State, StringComparer.OrdinalIgnoreCase))
               .OrderBy(r => r.SubmittedAt ?? DateTimeOffset.MinValue)
               .ThenBy(r => r.Id)
               .LastOrDefault()
               ?.State;
    }

    internal static bool IsApprovedBy(IEnumerable<Review> reviews, string login) =>
        string.Equals(CurrentStateOf(reviews, login), ReviewState.Approved, StringComparison.OrdinalIgnoreCase);
}
