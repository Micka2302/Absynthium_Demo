# Absynthium_Demo

Plugin CounterStrikeSharp pour enregistrer automatiquement les démos CS2, les compresser et les envoyer sur Discord ou un serveur FTP. Version **3.0.0**, DLL **`Absynthium_Demo.dll`**.

[Dépôt](https://github.com/Micka2302/Absynthium_Demo) · [Versions](https://github.com/Micka2302/Absynthium_Demo/releases) · [Migration](MIGRATION.md) · [Configuration complète](examples/Absynthium_Demo.json)

## Enregistrement et commandes

- Démarrage automatique lorsque `auto-record.min-player-start-record` est atteint et que les conditions de warmup sont satisfaites. Activé par défaut pour les nouvelles installations.
- Une annonce de lancement dans le chat ; une annonce de fin lorsque l'enregistrement est arrêté en fin de match ou lors d'un changement de carte.
- `!demo` dans le chat ou `css_demo` dans la console affiche le nom de la démo suivie, avec `.dem`, ou indique qu'aucune démo n'est en cours. La réponse est réservée à l'auteur de la commande. Cette commande ne démarre pas d'enregistrement et ne demande pas d'upload.
- Avec `auto-record.crop-rounds: false`, une démo continue couvre les rounds de la carte. L'option `true` conserve le découpage par round et annonce chaque nouveau démarrage.
- Arrêt sur inactivité, règles de warmup, noms personnalisés, compression ZIP, uploads Discord/FTP, suppression locale et rétention FTP restent disponibles.
- Les résultats des uploads sont inscrits dans les logs du serveur avec `general.log-uploads: true`.

Il n'y a aucun mode `demo-request` : les uploads configurés sont traités automatiquement après l'arrêt et selon `minimum-demo-duration`.

## Installation

Compiler avec le SDK .NET 10 via `./compile.ps1` ou `compile.cmd`. L'archive `compiled/Absynthium_Demo.zip` contient le plugin, ses dépendances, les traductions et un exemple de configuration. Extraire son dossier `counterstrikesharp` dans `game/csgo/addons/`.

La compilation produit également le plugin sans ZIP dans `compiled/counterstrikesharp/plugins/Absynthium_Demo/`, avec la DLL, les dépendances et les traductions. Copier ce dossier dans `game/csgo/addons/counterstrikesharp/plugins/` pour une installation directe. Ce dossier de sortie est remplacé à chaque compilation afin d'éviter de conserver d'anciens fichiers.

Le projet cible CounterStrikeSharp API **1.0.375**, .NET **10** et FluentFTP **53.0.2**. Le serveur doit disposer d'une installation CounterStrikeSharp complète et compatible, avec son runtime et Metamod. Mettre à jour seulement la DLL du plugin ne met pas à jour ces composants.

Configurer `tv_enable 1` avant le chargement de la map et `tv_autorecord 0`. La configuration se trouve dans `addons/counterstrikesharp/configs/plugins/Absynthium_Demo/Absynthium_Demo.json`. Les démos vont par défaut dans `game/csgo/discord_demos` et utilisent un chemin absolu pour éviter une redirection sous Metamod.

Pour une installation existante, suivre [MIGRATION.md](MIGRATION.md) : désactiver l'ancien plugin, reprendre la configuration et l'historique de rétention FTP. Le script `migrate-config.ps1` conserve les réglages reconnus et peut intégrer l'ancien payload personnalisé.

## Message Discord dans la configuration

Le modèle complet est désormais un objet JSON dans **`discord.payload`**. Aucun fichier `payload.json` séparé n'est nécessaire. Les options `webhook-name`, `webhook-avatar`, `embed-title` et `message-text` alimentent le modèle par des variables et restent personnalisables.

La présentation par défaut regroupe la carte, la durée, le nombre de joueurs, le round, la date, la taille et le nom du fichier. Le lien FTP et l'avertissement de taille apparaissent seulement lorsqu'ils sont disponibles. `message-text` est vide par défaut : aucune mention générale n'est ajoutée automatiquement. Une valeur personnalisée, y compris une mention, est conservée.

Pour changer la présentation, modifier les titres et l'ordre des champs dans `discord.payload.embeds[0].fields`. Les valeurs sont remplacées avant sérialisation JSON, ce qui préserve les guillemets, accents et retours à la ligne. Un champ dont la valeur résolue est vide est omis.

| Variable | Contenu |
| --- | --- |
| `{webhook_name}`, `{webhook_avatar}` | Nom et avatar du webhook |
| `{embed_title}`, `{message_text}` | Titre et message configurés |
| `{server_name}`, `{map}` | Serveur et carte |
| `{length}` | Durée en `hh:mm:ss` |
| `{round}`, `{player_count}` | Round et nombre de joueurs lors de l'arrêt |
| `{date}`, `{time}`, `{timedate}` | Date et heure locales du serveur |
| `{iso_timestamp}` | Horodatage UTC au format ISO |
| `{fileName}` | Nom de la démo sans extension |
| `{fileSizeInKB}` | Taille de l'archive ZIP en Ko |
| `{ftp_link}` | Adresse FTP après upload, vide sinon |
| `{file_size_warning}` | Avertissement si l'archive dépasse la limite configurée |

Les modèles de noms de fichiers acceptent `{fileName}`, `{map}`, `{date}`, `{time}`, `{timestamp}`, `{round}` et `{playerCount}`. `general.default-file-name` vaut `Absynthium_Demo` par défaut ; une valeur existante est conservée lors de la migration.

## Vérification et limites

```powershell
dotnet build Absynthium_Demo.sln -c Release
dotnet run --project tests/Absynthium_Demo.RegressionTests.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Migration.Tests.ps1
./compile.ps1
```

Les tests couvrent les chemins absolus, les fichiers encore en écriture, la finalisation, la compression, les collisions, le rendu du payload et la reprise des réglages. Les étapes de validation en jeu figurent dans [MIGRATION.md](MIGRATION.md).

Le plugin conserve le fonctionnement sans sondage de fichier ni retry périodique du diagnostic précédent. L'annonce de lancement et `!demo` reflètent la commande suivie par le plugin ; ils ne confirment pas que le moteur a ouvert le fichier. En cas d'échec moteur, arrêter avec `tv_stoprecord`, corriger le problème et relancer. Le diagnostic historique reste consultable dans [DIAGNOSTIC.md](DIAGNOSTIC.md).

L'upload attend un fichier stable et accessible après `tv_stoprecord`. Les démos actives et les uploads en cours sont exclus du nettoyage. `minimum-demo-duration` filtre les uploads courts. Les options `delete-demo-after-upload` et `delete-zipped-demo-after-upload` restent applicables.

## Licence et origine

Projet dérivé de [K4-GOTV par K4ryuu / KitsuneLab](https://github.com/KitsuneLab-Development/K4-GOTV), distribué sous GPL-3.0. Voir [LICENSE.md](LICENSE.md). Les noms historiques dans le changelog et le diagnostic sont conservés pour identifier les anciennes versions.
