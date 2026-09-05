using CommandLine;

namespace Krosoft.Github.CLI;

internal static class Options
{
    [Verb("pr-list", HelpText = "Liste les pull requests d'un owner GitHub selon les filtres du profil.")]
    internal class PullRequestsOptions
    {
        [Option('p', "profile", Required = true, HelpText = "Chemin vers le fichier de profil JSON.")]
        public string Profile { get; set; } = string.Empty;
    }

    [Verb("pr-approve", HelpText = "Approuve toutes les pull requests correspondant aux filtres du profil.")]
    internal class ApproveOptions
    {
        [Option('p', "profile", Required = true, HelpText = "Chemin vers le fichier de profil JSON.")]
        public string Profile { get; set; } = string.Empty;

        [Option('d', "dry-run", Required = false, Default = false, HelpText = "Affiche les pull requests qui seraient approuvées, sans rien modifier.")]
        public bool DryRun { get; set; }

        [Option('n', "numbers", Required = false, Separator = ',', HelpText = "Limite l'approbation à ces numéros de pull request (ex: --numbers 42,43). Remplace les filtres titre/dépôt du profil.")]
        public IEnumerable<int> Numbers { get; set; } = [];

        [Option('m', "merge", Required = false, Default = false, HelpText = "Fusionne les PR correspondantes après approbation (méthode : github.mergeMethod, défaut squash).")]
        public bool Merge { get; set; }
    }

    [Verb("pr-merge", HelpText = "Fusionne les pull requests correspondant aux filtres du profil (les PR non fusionnables sont ignorées).")]
    internal class MergeOptions
    {
        [Option('p', "profile", Required = true, HelpText = "Chemin vers le fichier de profil JSON.")]
        public string Profile { get; set; } = string.Empty;

        [Option('d', "dry-run", Required = false, Default = false, HelpText = "Affiche les pull requests qui seraient fusionnées, sans rien modifier.")]
        public bool DryRun { get; set; }

        [Option('n', "numbers", Required = false, Separator = ',', HelpText = "Limite la fusion à ces numéros de pull request (ex: --numbers 42,43). Remplace les filtres titre/dépôt du profil.")]
        public IEnumerable<int> Numbers { get; set; } = [];
    }

    [Verb("run-list", HelpText = "Liste les runs GitHub Actions en cours et en attente des dépôts du profil.")]
    internal class RunListOptions
    {
        [Option('p', "profile", Required = true, HelpText = "Chemin vers le fichier de profil JSON.")]
        public string Profile { get; set; } = string.Empty;
    }

    [Verb("pr-rerun", HelpText = "Relance les runs GitHub Actions en échec des pull requests correspondant aux filtres du profil.")]
    internal class RerunOptions
    {
        [Option('p', "profile", Required = true, HelpText = "Chemin vers le fichier de profil JSON.")]
        public string Profile { get; set; } = string.Empty;

        [Option('d', "dry-run", Required = false, Default = false, HelpText = "Affiche les runs qui seraient relancés, sans rien modifier.")]
        public bool DryRun { get; set; }

        [Option('n', "numbers", Required = false, Separator = ',', HelpText = "Limite la relance à ces numéros de pull request (ex: --numbers 42,43). Remplace les filtres titre/dépôt du profil.")]
        public IEnumerable<int> Numbers { get; set; } = [];
    }
}
