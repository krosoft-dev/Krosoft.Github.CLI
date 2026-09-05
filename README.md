# Krosoft.Github.CLI

[![forthebadge](https://forthebadge.com/badges/built-with-love.svg)](https://forthebadge.com) [![forthebadge](https://forthebadge.com/badges/made-with-c-sharp.svg)](https://forthebadge.com)

Outil CLI pour gérer en masse les pull requests d'un owner GitHub (organisation ou utilisateur) : lister, approuver, fusionner, relancer les runs GitHub Actions en échec.

## Installation

```bash
dotnet pack .
dotnet tool install --global --add-source ./publish Krosoft.Github.CLI
```

La commande installée s'appelle `krosoft-github`. Le script `tools/scripts/dotnet_install_cli.ps1` enchaîne build, pack et réinstallation. Sans installer, chaque commande se lance aussi via `dotnet run` (voir plus bas).

## Profil

Toutes les commandes prennent `--profile <fichier.json>`. Le profil cible un owner et définit les PR concernées.

```json
{
  "name": "tenor",
  "github": {
    "owner": "mon-organisation",
    "pat": "ghp_xxxxxxxx",
    "repositories": [],
    "mergeMethod": "squash"
  },
  "pullRequests": {
    "state": "open",
    "titles": ["Renovate - Update all Krosoft.Extensions packages"],
    "exactTitle": true,
    "repositories": []
  }
}
```

| Champ | Description |
|-------|-------------|
| `owner` | Organisation ou utilisateur GitHub à parcourir (ex: `krosoft-dev`). |
| `pat` | Personal Access Token. Classique : scopes `repo` (dépôts privés) ou `public_repo`, et `workflow` (pour relancer les runs). Fine-grained : *Pull requests* (read & write), *Actions* (read & write), *Contents* (read), *Metadata* (read). |
| `repositories` | Dépôts à parcourir (scope du scan). Vide = tous les dépôts non archivés de l'owner. |
| `apiUrl` | Optionnel. URL de base de l'API pour GitHub Enterprise (ex: `https://github.mon-entreprise.com/api/v3`). Défaut : `https://api.github.com`. |
| `mergeMethod` | Méthode de fusion utilisée par `pr-merge` : `squash` (défaut), `merge` ou `rebase`. Doit être autorisée par le dépôt. |
| `state` | `open` (défaut), `closed` ou `all`. |
| `titles` | Titres recherchés (OU, insensible à la casse). Vide = tous. |
| `exactTitle` | `true` : titre égal. `false` : titre contenant. |
| `pullRequests.repositories` | Filtre secondaire sur le nom du dépôt, appliqué après le scan. En général laissé vide. |

`files/local.json` est un exemple versionné. Les autres `files/*.json` sont ignorés par git : y mettre les profils avec un vrai PAT.

## Commandes

| Commande | Rôle |
|----------|------|
| `pr-list` | Liste les PR correspondant au profil. |
| `pr-approve` | Approuve les PR (les PR déjà approuvées sont ignorées). Avec `--merge`, fusionne dans la foulée. |
| `pr-merge` | Fusionne les PR selon `mergeMethod` (les brouillons sont ignorés ; une PR non fusionnable — checks/approbations manquantes, conflit — est signalée en erreur). |
| `pr-rerun` | Relance les runs GitHub Actions en échec du dernier commit des PR (équivalent du bouton *Re-run failed jobs*). Un seul run par workflow est conservé (le plus récent). |
| `run-list` | Liste les runs GitHub Actions en cours et en attente sur les dépôts du profil (les filtres `pullRequests` ne s'appliquent pas). |

> **Approuver ne fusionne pas.** Contrairement à l'*auto-complete* d'Azure DevOps, approuver une PR sur GitHub ne la fusionne jamais. Pour fusionner, il faut soit configurer Renovate en `automerge`, soit activer l'*auto-merge* GitHub sur la PR, soit fusionner explicitement. Le plus simple : `pr-approve --merge` (approuve puis fusionne). `pr-merge` seul fusionne sans réapprouver.

`pr-approve`, `pr-merge` et `pr-rerun` acceptent :

| Option | Description |
|--------|-------------|
| `--dry-run`, `-d` | Affiche ce qui serait fait, sans rien modifier. |
| `--numbers`, `-n` | Limite aux PR indiquées (`--numbers 42,43`). Remplace les filtres `titles`/`repositories`. Attention : un numéro de PR n'est unique qu'au sein d'un dépôt ; si le scan couvre plusieurs dépôts, un même numéro peut correspondre à plusieurs PR. |

`pr-approve` accepte en plus :

| Option | Description |
|--------|-------------|
| `--merge`, `-m` | Fusionne les PR correspondantes juste après l'approbation (méthode `github.mergeMethod`). En cas d'échec d'une approbation, la fusion n'est pas enchaînée. |

Avec l'outil installé :

```bash
krosoft-github pr-list --profile ./files/tenor.json
krosoft-github pr-approve --profile ./files/tenor.json --dry-run
krosoft-github pr-approve --profile ./files/tenor.json
krosoft-github pr-approve --profile ./files/tenor.json --merge
krosoft-github pr-merge --profile ./files/tenor.json --dry-run
krosoft-github pr-merge --profile ./files/tenor.json
krosoft-github pr-rerun --profile ./files/tenor.json --numbers 42
krosoft-github run-list --profile ./files/tenor.json
```

Depuis les sources (le `--` sépare les arguments de `dotnet run` de ceux du CLI) :

```bash
dotnet run --project src/Krosoft.Github.CLI -- pr-list --profile ./files/tenor.json
dotnet run --project src/Krosoft.Github.CLI -- pr-approve --profile ./files/tenor.json --dry-run
dotnet run --project src/Krosoft.Github.CLI -- pr-approve --profile ./files/tenor.json
dotnet run --project src/Krosoft.Github.CLI -- pr-merge --profile ./files/tenor.json --dry-run
dotnet run --project src/Krosoft.Github.CLI -- pr-merge --profile ./files/tenor.json
dotnet run --project src/Krosoft.Github.CLI -- pr-rerun --profile ./files/tenor.json --numbers 42
dotnet run --project src/Krosoft.Github.CLI -- run-list --profile ./files/tenor.json
```

Code de sortie : `0` si tout s'est bien passé, `-1` en cas d'erreur ou si au moins une action a échoué.

> Une PR avec *auto-merge* activé est fusionnée dès que ses conditions sont remplies : vérifier en `--dry-run` avant d'approuver.
