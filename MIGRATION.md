# Migration vers Absynthium_Demo 3.0.0

Le plugin et son assembly portent exactement le nom `Absynthium_Demo`. La configuration passe à la version 14. Le dépôt est disponible à l'adresse https://github.com/Micka2302/Absynthium_Demo.

## Installation depuis K4-GOTV

1. Arrêter le serveur. Sauvegarder l'ancien dossier `addons/counterstrikesharp/plugins/K4-GOTV`, sa configuration et les démos à conserver.
2. Déplacer l'ancien dossier du plugin **hors de `plugins`** pour ne pas charger les deux DLL en même temps. L'archive n'effectue pas cette opération.
3. Extraire le dossier `counterstrikesharp` de `Absynthium_Demo.zip` dans `game/csgo/addons/`. Le plugin doit se trouver dans `addons/counterstrikesharp/plugins/Absynthium_Demo/Absynthium_Demo.dll` avec `FluentFTP.dll` et le dossier `lang`.
4. Migrer la configuration existante vers `addons/counterstrikesharp/configs/plugins/Absynthium_Demo/Absynthium_Demo.json`. L'outil ci-dessous conserve les réglages reconnus (webhook, FTP, seuils, noms, suppressions, rétention), ajoute les nouveaux champs et retire les anciennes options inutilisées comme `demo-request`. Il refuse d'écraser un fichier existant.
5. Si la rétention FTP était utilisée, recopier `uploads_retention.json` de l'ancien dossier du plugin dans le nouveau.
6. Vérifier `auto-record.enabled: true` et les seuils de joueurs. Une valeur `false` explicitement présente dans l'ancienne configuration est conservée par l'outil. Configurer `tv_enable 1` avant le chargement de la map et `tv_autorecord 0` pour laisser le plugin gérer les démos.
7. Redémarrer et vérifier `Absynthium_Demo 3.0.0` dans `css_plugins list`.

Depuis le dossier du projet ou celui de l'archive, sous PowerShell :

```powershell
./migrate-config.ps1 -SourceConfig "C:/serveur/game/csgo/addons/counterstrikesharp/configs/plugins/K4-GOTV/K4-GOTV.json" -DestinationConfig "C:/serveur/game/csgo/addons/counterstrikesharp/configs/plugins/Absynthium_Demo/Absynthium_Demo.json"
```

Pour reprendre un ancien payload personnalisé, ajouter `-LegacyPayload "C:/serveur/game/csgo/addons/counterstrikesharp/plugins/K4-GOTV/payload.json"` en indiquant son emplacement sauvegardé. Sans cette option, l'outil utilise la nouvelle présentation (ou le `discord.payload` déjà présent). Aucun fichier `payload.json` n'est chargé par le nouveau plugin.

Sans PowerShell, partir de `examples/Absynthium_Demo.json` et reporter les sections `general`, `auto-record`, `ftp` et les réglages Discord existants. Garder `discord.payload`, ou y placer directement l'objet JSON de l'ancien payload, puis fixer `ConfigVersion` à `14`.

Pour une installation neuve, la configuration par défaut est générée par CounterStrikeSharp. L'archive garde l'exemple dans `examples` afin de ne pas écraser une configuration de serveur.

## Vérifications en jeu

- Sous le seuil de joueurs : aucun démarrage automatique ; `!demo` indique qu'aucune démo n'est en cours.
- Seuil atteint et conditions de warmup satisfaites : une annonce de lancement. `!demo` affiche uniquement à son auteur le nom avec l'extension `.dem`.
- Avec `crop-rounds: false`, les rounds et arrivées de joueurs suivants ne répètent pas l'annonce.
- À la fin du match : une annonce de fin. Le changement de carte suivant ne doit pas la doubler ; `!demo` ne doit plus afficher l'ancien fichier.
- Sur la nouvelle carte : nouveau nom de démo lorsque les conditions de démarrage sont satisfaites.
- Après arrêt : vérifier le fichier final, sa lecture et l'upload Discord/FTP si configuré. Les logs d'upload restent dans la console (`general.log-uploads: true`).

Le suivi conserve le fonctionnement sans sondage périodique de la version précédente : le nom affiché est celui de l'enregistrement suivi par le plugin. Une commande moteur échouée n'est pas automatiquement détectée. Après correction du problème moteur, `tv_stoprecord` libère l'état, puis un démarrage automatique ou `tv_record` peut réessayer. Les tests locaux ne valident ni la capture réelle ni les performances de CS2.

Les réglages de suppression sont conservés ; ils restent à vérifier avant le redémarrage, notamment `delete-every-demo-from-server-after-server-start`. Les traductions utilisent désormais les clés `absynthium_demo.*` : installer le dossier `lang` fourni, puis reporter les personnalisations éventuelles.
