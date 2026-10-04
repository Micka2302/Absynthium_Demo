# K4-GOTV 2.1.7-diagnostic.1

Cette variante sert à comparer la fluidité avec la version 2.1.6. La disparition des saccades n'a pas encore été vérifiée sur un serveur CS2.

## Résultat du test rapporté le 26 septembre 2026

Le serveur affiche bien `2.1.7-diagnostic.1`, mais les saccades persistent dès le démarrage de l'enregistrement. Elles se reproduisent également avec un enregistrement manuel après déchargement de K4-GOTV, puis disparaissent avec `tv_stoprecord`. La suppression des timers ne constitue donc pas un correctif du problème observé.

L'utilisateur précise ensuite avoir déjà désactivé tous les plugins Metamod la veille, sans disparition du problème. Il confirme que Metamod lui-même était toujours chargé. Ne pas lui redemander de tester Game Fixes, StatusModifier ou CounterStrikeSharp individuellement. Le test de CS2 sans Metamod dès le démarrage n'a donc pas encore été effectué.

L'utilisateur confirme que ses configurations n'ont pas changé. L'enquête porte désormais sur les mises à jour CS2 / CounterStrikeSharp / Metamod, sans modification des limites ou réglages de ses serveurs.

### Changements amont vérifiés

- Valve annonce une mise à jour du moteur vers la dernière version de Source 2 dans les notes du 22 septembre 2026, récupérées via son [API Steam News](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=730&count=15&maxlength=12000&feeds=steam_community_announcements).
- [CounterStrikeSharp 1.0.375, publié le 24 septembre](https://github.com/roflmuffin/CounterStrikeSharp/releases/tag/v1.0.375), intègre la [migration vers KHook](https://github.com/roflmuffin/CounterStrikeSharp/pull/1418), fusionnée le 23 septembre. Les interceptions des commandes, événements, messages réseau, communications vocales et frames ont été migrées.
- Cette version intègre aussi les [adaptations à CS2 1.41.8.2](https://github.com/roflmuffin/CounterStrikeSharp/pull/1433), dont le SDK moteur et les signatures/offsets. Le passage de l'API 342 à 375 et de .NET 8 à 10 dans K4-GOTV est distinct de ces changements natifs.
- Comparaison des extractions de variables CS2 du [9 septembre](https://github.com/SteamDatabase/GameTracking-CS2/blob/d8e2c7a4f9b86e60d5a1b584a83ee0e15f59cc54/DumpSource2/convars.txt) et du [25 septembre](https://github.com/SteamDatabase/GameTracking-CS2/blob/3fc98e763328f7d1627405b389d1b6b69c5b0e38/DumpSource2/convars.txt) : les lignes existantes `tv_*` et `demo_*` comparées ne changent pas. Une variable de lecture, `tv_playcast_slow_playback_fragment_count`, apparaît. Cela ne prouve pas que le comportement interne de l'enregistrement soit inchangé.
- La mise à jour KHook intégrée par Metamod le 25 septembre porte sur l'arrêt de ses threads de retrait des hooks ([commit](https://github.com/Kenzzer/KHook/commit/2a8953533da2dd473191f35448e21fcbe5667cc2)). Elle n'est pas identifiée comme un correctif des saccades à l'enregistrement.

Aucune cause précise ni aucun correctif de ces saccades n'est confirmé par cette inspection. Ne pas présenter une autre recompilation de K4-GOTV ou un changement de configuration comme une solution démontrée.

### Binaires chargés et piste native

La sortie serveur fournie confirme Metamod `2.0.0-dev+1472` (`05c5c63`) et les extensions suivantes : CounterStrikeSharp `1.0.375 @ 751eb0c`, MultiAddonManager `1.6.1-0-gcf61a1c`, Game Fixes `1.0.9 @ 43d83a1`, StatusModifier `a702765-khook-mm1469-local`. La sortie de la commande CS2 `version` n'a pas été fournie.

Inspection de Game Fixes au commit exact `43d83a1` :

- Le correctif [demo_record](https://github.com/SlynxCZ/GameFixes_mm/blob/43d83a1/src/fixes/demo_record.cpp), s'il est activé, intercepte `CServerSideClient::Disconnect` pour bloquer la déconnexion de GOTV lorsque `tv_enable` est actif. Il ajoute aussi une place à la session au démarrage.
- Le correctif [workshop_voice](https://github.com/SlynxCZ/GameFixes_mm/blob/43d83a1/src/fixes/workshop_voice.cpp), s'il est activé, intercepte globalement `CServerSideClient::SendNetMessage`, filtre les messages vocaux et réécrit leur XUID par destinataire.
- Ces interceptions utilisent KHook. Leur présence est une piste à isoler, pas une preuve de responsabilité. Les options effectivement activées sur le serveur ne sont pas connues.

La proposition de tester Game Fixes seul est désormais dépassée par le résultat communiqué : les saccades persistent avec tous les plugins Metamod désactivés. La recherche se concentre sur l'enregistrement natif après la mise à jour CS2 et sur Metamod lui-même, dont le chargement était conservé. Aucun correctif amont correspondant précisément à ce symptôme n'a été identifié dans les recherches effectuées.

### Prochain test : CS2 sans chargement de Metamod

Procédure proposée, non exécutée sur le serveur :

1. Arrêter complètement le serveur dans Pterodactyl et sauvegarder `game/csgo/gameinfo.gi`.
2. Retirer temporairement la ligne `Game csgo/addons/metamod` de la section `SearchPaths`. Ce chemin de chargement est documenté par [AlliedModders](https://wiki.alliedmods.net/Installing_SourceMM).
3. Démarrer le serveur. Dans sa console, `meta version` doit être une commande inconnue. Si elle répond encore, le test sans Metamod n'est pas établi ; vérifier notamment si le script de démarrage a réinséré la ligne.
4. Conserver les réglages GOTV et reproduire dans des conditions de jeu comparables avec `tv_record diagnostic_sans_metamod_01` (choisir un autre nom si ce fichier existe déjà), puis `tv_stoprecord` après observation.
5. Arrêter le serveur, restaurer le fichier sauvegardé et redémarrer pour rétablir le chargement habituel.

Si les saccades persistent, l'enregistrement natif reproduit le problème sans Metamod ni ses plugins. Si elles disparaissent, confirmer ensuite la différence sur un démarrage avec Metamod seul et aucun plugin avant d'attribuer la régression à Metamod. Le résultat de ce test est encore attendu.

## Changements ciblés

- Suppression de la vérification du fichier toutes les secondes au démarrage.
- Suppression de l'arrêt forcé lorsque cette vérification expire.
- Suppression de la relance automatique toutes les cinq secondes.
- Le message aux joueurs indique une demande d'enregistrement, sans confirmer la création du fichier.

Les chemins absolus, noms configurés, dépendances (.NET 10, CounterStrikeSharp API 1.0.375), paramètres GOTV, arrêt sur inactivité et traitement après arrêt restent identiques à la version 2.1.6. Les vérifications de collision des noms au démarrage sont conservées : cette variante retire la surveillance répétée, pas tous les accès disque.

## Installation de l'archive de mise à jour

1. Arrêter le serveur et sauvegarder le dossier `game/csgo/addons/counterstrikesharp/plugins/K4-GOTV` pour pouvoir revenir à la version précédente.
2. Copier le dossier `counterstrikesharp` de l'archive dans `game/csgo/addons/`, en fusionnant les dossiers et en remplaçant les fichiers fournis. L'archive contient uniquement `K4-GOTV.dll` et les traductions `lang/en.json` et `lang/pl.json`. Si les traductions sont personnalisées, conserver les préfixes personnalisés et reporter uniquement le texte de `k4.demo.start`.
3. Garder la configuration du plugin, `payload.json`, `FluentFTP.dll` et les réglages GOTV existants. Cette archive est une mise à jour d'une installation 2.1.6, pas une installation complète.
4. Redémarrer et vérifier que `css_plugins list` affiche `2.1.7-diagnostic.1`.

La configuration fournie active `delete-every-demo-from-server-after-server-start` : les anciennes démos et archives seront donc supprimées au chargement, comme avant. Sauvegarder les enregistrements à conserver avant le redémarrage.

## Comparaison sans logs

- Garder la même map, les mêmes paramètres et un nombre de joueurs comparable. Avec la configuration fournie, il faut au moins deux joueurs humains pour déclencher l'enregistrement automatique.
- Jouer plusieurs minutes, au-delà de 30 secondes, et comparer les saccades à la version 2.1.6.
- Vérifier que le fichier `.dem` dans le dossier configuré est créé et grossit. Le message en jeu indique seulement que la commande a été demandée.
- Avec `crop-rounds: false`, vérifier que l'enregistrement continue entre les rounds. Tester également un changement de map.
- Arrêter la démo avec `tv_stoprecord`, attendre sa finalisation et vérifier sa lecture. Le prochain début de round peut démarrer une autre démo si les conditions sont réunies.
- Vérifier l'arrêt après plus de 300 secondes sous deux joueurs, conformément à la configuration fournie.

Un démarrage échoué reste réservé dans cette version : il faut `tv_stoprecord` puis `tv_record` pour réessayer, après avoir résolu le problème moteur. Un début de round seul ne réinitialise pas une demande échouée. L'absence du message `Demo recording confirmed` est normale.

Pour revenir à 2.1.6, arrêter le serveur, remettre la DLL et les traductions sauvegardées, puis redémarrer.

## Vérifications locales

La publication Release et les tests existants de chemins, fichiers verrouillés, finalisation et compression ont réussi. Ils ne mesurent pas la fluidité et ne remplacent pas la lecture d'une démo dans CS2.
