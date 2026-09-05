namespace Krosoft.Github.CLI.Models;

// Méthodes de merge acceptées par l'API GitHub (champ "merge_method").
internal static class MergeMethods
{
    internal const string Squash = "squash";
    internal const string Merge = "merge";
    internal const string Rebase = "rebase";

    // Défaut : squash, la méthode la plus courante pour les PR Renovate.
    internal const string Default = Squash;

    internal static readonly string[] Allowed = [Squash, Merge, Rebase];

    internal static bool IsValid(string? method) =>
        method is not null && Allowed.Contains(method, StringComparer.OrdinalIgnoreCase);

    internal static string Resolve(string? method) =>
        IsValid(method) ? method!.ToLowerInvariant() : Default;
}
