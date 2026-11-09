# Guide des agents — InputOS

Ce fichier s'applique à l'ensemble du dépôt. Il décrit les règles de travail et l'état du projet observé le 29 octobre 2026. Le code et les scripts réellement présents restent la référence : relire les fichiers concernés et actualiser ce guide si le comportement change. Distinguer une fonctionnalité implémentée, un essai synthétique et une validation sur une vraie manette.

## Préférences et règles de travail

- Répondre en français, simplement et précisément. Expliquer les changements, leur validation et les limites réelles.
- Ne jamais utiliser le skill `computer-use` ni ses outils de contrôle d'interface. Utiliser les connecteurs spécialisés et les outils adaptés autorisés, par exemple Roblox Studio MCP lorsque pertinent.
- Travailler sur `dev` pour InputOS, sauf instruction ultérieure explicite. Vérifier `git branch --show-current` et `git status --short` avant toute modification. Ne pas effacer, écraser, remettre à zéro ou déplacer les changements de l'utilisateur.
- Partir des fichiers actuels, non d'une ancienne version recopiée. Modifier directement le projet local. Si l'utilisateur demande le contenu d'un fichier, fournir le fichier complet selon sa préférence ; ne pas remplacer un fichier complet par une reconstruction partielle.
- Conserver les fonctionnalités existantes : détection, testeur, zone de notification, batterie DualShock 4, configuration, profils locaux, diagnostics, paramètres, mises à jour et migration. Éviter les refontes sans rapport avec la demande.
- Faire les vérifications adaptées à la modification. Après une modification du code ou du XAML, valider au minimum la compilation lorsque l'environnement le permet ; ajouter des essais ciblés et indiquer les tests matériels non effectués. Pour une modification purement documentaire, vérifier son contenu et ses chemins.
- Ne pas publier de release, pousser, fusionner, créer d'issue, envoyer de diagnostic réel, installer ou désinstaller l'application sans instruction ou autorisation appropriée. Les tests de lecture et les modifications locales réversibles nécessaires à la tâche ne demandent pas de confirmation supplémentaire.
- Ne pas copier des identifiants privés, chemins de périphérique, noms de compte ou secrets dans les diagnostics publics, journaux partagés ou fichiers suivis. Ne pas embarquer de jeton GitHub ou secret Cloudflare dans l'application.
- Ne pas prétendre qu'une manette est compatible sur tous les transports ou toutes les révisions à partir de son seul nom. Vérifier VID, PID, connexion, nombre d'entrées et rapports de diagnostic.
- Pour toute modification, ouvrir une session dédiée à la catégorie de la modification.
- Ne pas lancer de build de publication (release) sans demande explicite de l'utilisateur, qui indiquera quand publier une mise à jour. Lors d'une telle demande, accompagner le build d'un devlog destiné à GitHub.
- Lors d'une demande de publication, le build de release doit rester compatible avec Velopack : packId `InputOS`, canal `win`, version `major.minor.patch` strictement supérieure à la précédente, et paquet `-full.nupkg` de la version précédente présent dans `Releases/<version>/` pour générer le delta et l'index `releases.win.json`. Publier ensuite tous les fichiers du dossier dans une release GitHub stable. Une build de développement (`bin/Debug`) ou une copie non installée par Velopack ne sert pas à tester la mise à jour (`IsInstalled` est faux). Voir `Packaging/Build-Release.ps1` et les index de `Releases/1.1.1/` (`RELEASES`, `releases.win.json`) comme référence.

## Présentation et technologie

InputOS est une application Windows de détection, identification et test de manettes. Elle propose un testeur en direct et un parcours permettant de suggérer un profil non officiel, de l'utiliser localement et de transmettre un diagnostic pour validation. Les manettes détectées ne possèdent pas nécessairement un profil officiel.

- Projet : `InputOS.csproj`, version déclarée actuellement `1.1.1`, `WinExe`, cible `net10.0-windows10.0.19041.0`.
- Windows 10/11, distribution Windows x64 ; interface WPF et Windows Forms pour la zone de notification.
- SDK .NET 10 ; le SDK local observé est `10.0.401`. Les références Windows Desktop et Windows SDK doivent être disponibles.
- Dépendances : `HidSharp` 2.6.4 et `Velopack` 1.2.0. La version de ces dépendances et le SDK doivent être revérifiés dans le projet et l'environnement avant une release.
- `Nullable` et `ImplicitUsings` sont activés. Namespace principal : `InputOS` ; les services utilisent `InputOS.Services`, les modèles `InputOS.Models`.
- Le fichier `Properties/GlobalUsing.cs` contient des alias WPF afin de résoudre les ambiguïtés avec WinForms. Les conserver lors d'un changement d'imports.
- `Program.cs` est le point d'entrée `[STAThread]`. Il appelle `VelopackApp.Build().Run()` avant de créer et initialiser `App`. Conserver cet ordre : il permet l'application des mises à jour.
- `App.xaml` est inclus comme `Page` et retiré de `ApplicationDefinition` dans le projet pour conserver le point d'entrée personnalisé. `App.xaml.cs` ouvre `MainWindow`, démarre le service de mise à jour et l'arrête à la fermeture.
- Icône applicative et installateur : `Assets/Icons/iOSBlack.ico`. Les ressources d'icônes et images sont embarquées dans le projet.

## Organisation des fichiers

| Chemin | Responsabilité |
| --- | --- |
| `App.xaml`, `App.xaml.cs`, `Program.cs` | Ressources WPF, démarrage et arrêt. |
| `Views/Windows/MainWindow.xaml(.cs)` | Liste et sélection des manettes, informations, suggestion, testeur, événements de connexion et zone de notification. |
| `Views/Windows/TesterWindow.xaml(.cs)` | Lecture et visualisation des boutons, axes, sticks, gâchettes et croix ; inversion visuelle Y. |
| `Views/Windows/SettingsWindow.xaml(.cs)` | Paramètre de fermeture dans la zone de notification et état/commandes de mise à jour. |
| `Views/Windows/ControllerSuggestion/ControllerSuggestionWindow.xaml(.cs)` | Parcours de configuration, détection et lecture en direct. |
| `ControllerSuggestionWindow.Buttons.cs` | Identification et aperçu des commandes, schéma et microphone. |
| `ControllerSuggestionWindow.Export.cs` | Enregistrement local et envoi explicite du diagnostic. |
| `Models/` | Paramètres, brouillon, résultats détectés, axes calibrés, définitions et mappings de commandes, profil local. |
| `Services/Controllers/ControllerCompatibilityService.cs` | Profils officiels et compatibilité centralisée. |
| `ControllerAutoDetectionService.cs` | Instantané Windows des entrées et noms disponibles. |
| `ControllerLocalProfileService.cs` | Lecture et validation des profils locaux. |
| `ControllerProfileExportService.cs` | JSON de profil, enregistrement atomique et anciennes fonctions d'issue préremplie. |
| `ControllerDiagnosticService.cs` | Envoi HTTPS au service de diagnostics. |
| `JoystickConfigurationService.cs` | Identification guidée et calibration des sticks. |
| `ControllerButtonCatalogService.cs`, `ControllerButtonConfigurationService.cs`, `ControllerButtonStateService.cs` | Catalogue des commandes, association et interprétation des entrées. |
| `ControllerDiagramService.cs` | Dessins vectoriels WPF et marqueurs interactifs des manettes. |
| `DualSenseProfileService.cs`, `XboxSeriesProfileService.cs` | Profils officiels associés aux diagnostics concernés. |
| `DualSenseMicrophoneReader.cs`, `NintendoProControllerReader.cs` | Lectures HID spécifiques. |
| `ControllerBatteryService.cs` | Informations de batterie DualShock 4. |
| `Services/Settings/SettingsService.cs` | Paramètres JSON de l'utilisateur. |
| `Services/Updates/ApplicationUpdateService.cs` | Vérification, téléchargement et application des mises à jour Velopack. |
| `Config/diagnostic-settings.json` | Adresse du service ; copié sous `diagnostic-settings.json` à côté de l'exécutable. |
| `Assets/`, `Properties/` | Icônes, images, ressources et configuration d'assembly. |
| `Packaging/` | Scripts locaux de publication Velopack et setup de migration Inno Setup, si disponibles. |
| `Releases/<version>/` | Artefacts générés localement et ignorés par Git. |
| `bin/`, `obj/` | Sorties de build, outils et vérifications temporaires ; ignorés par Git. |

Attention : le README mentionne `Cloudflare/` et `Packaging/Legacy/`, absents de l'arbre local observé. `Cloudflare/` et les quatre fichiers de `Packaging/` sont actuellement ignorés explicitement par `.gitignore`, même si les scripts de packaging existent localement. Ils peuvent manquer dans un clone neuf. Vérifier leur présence et leur suivi avant de s'appuyer dessus ; ne pas inventer les sources du service ou leurs secrets.

## Conventions de code et d'interface

- Respecter le style C# et XAML existant, notamment les blocs `FONCTIONNALITÉ`, `CONFIGURATION`, `LOGIQUE` et `FIN FONCTIONNALITÉ`, lorsque le fichier utilise ces conventions.
- Garder les noms, namespaces, `x:Class`, gestionnaires d'événements et ressources cohérents entre XAML et code-behind. Réutiliser les services au lieu de dupliquer la logique dans les fenêtres.
- Employer les ressources `AppBackgroundBrush`, `AppForegroundBrush`, `AppPanelBrush`, `AppBorderBrush`, `AppAccentBrush` et `AppSecondaryTextBrush`, et les styles existants. Garder une interface lisible et cohérente avec les fenêtres actuelles.
- Les commandes sont représentées par des géométries vectorielles WPF dans `ControllerDiagramService` : conserver les marqueurs et leur association aux identifiants, la mise en évidence en direct et les couleurs. Éviter de remplacer ces dessins par des caractères approximatifs ou des images fixes sans demande.
- Ne pas bloquer le thread UI avec la lecture HID, une détection longue, le réseau ou une installation. Préserver les annulations, la fermeture des flux, les événements et les temporisateurs.

## Détection et compatibilité

`MainWindow` énumère `RawGameController.RawGameControllers` de `Windows.Gaming.Input`, écoute les connexions/déconnexions et permet une actualisation manuelle. Elle affiche le nom, VID, PID, connexion et nombres de boutons/axes/switches. Si Windows ne présente pas de `RawGameController`, il n'existe actuellement aucun fallback HID autonome dans la liste principale : le lecteur Nintendo ne peut pas être créé depuis l'assistant.

La compatibilité est centralisée dans `ControllerCompatibilityService`. Vérifier le code avant de modifier les conditions : certains profils exigent un nombre précis d'entrées ou un transport précis. La présence d'un modèle dans la liste ou d'un couple VID/PID n'est pas une validation de toutes ses variantes. Une manette sans profil compatible affiche un avertissement et le bouton « Suggérer » ; le bouton du testeur est désactivé tant qu'aucun profil utilisable n'est disponible. Un profil officiel applicable prime sur un profil local valide.

| Modèle | Identifiants / portée observée | Points particuliers |
| --- | --- | --- |
| DualShock 4 | Sony `054C:05C4` et `054C:09CC` | Deux révisions matérielles, pas nécessairement deux profils distincts ; batterie prise en charge. |
| DualSense | Sony `054C:0CE6` | Profil officiel USB et Bluetooth dans le code ; vérifier séparément les rapports et le matériel de chaque connexion. |
| DualSense Edge | Sony `054C:0DF2` | Compatibilité et entrées selon les conditions du service ; lecture microphone HID spécifique. |
| Xbox Elite Series 2 | Microsoft `045E:0B22` | Vérifier les axes et conditions dans le service existant. |
| Xbox Series S/X | Microsoft `045E:0B12`, USB | Profil officiel pour la disposition observée : 14 boutons, 6 axes, 0 switch ; Guide et Share indiqués indisponibles dans les diagnostics. Ne pas extrapoler au Bluetooth. |
| Nintendo Switch Pro HAC-013 | Nintendo `057E:2009` | Lecteur USB spécifique en cours de développement ; profil officiel non validé, tests matériels encore nécessaires et défaut connu à l'étape des boutons ci-dessous. |

Les profils DualSense et Xbox Series reposent sur les diagnostics GitHub historiques : issues `#1` et `#3` pour DualSense, `#4` et `#6` pour Xbox. Avant d'intégrer un nouveau profil, vérifier les rapports réels, les différences de transport et de révision, la cohérence des axes, la croix, les gâchettes, les boutons spéciaux et les entrées indisponibles. Ne pas désactiver les profils existants pour tester sans restaurer l'état demandé par l'utilisateur.

## Lecture des manettes et particularités HID

- Le chemin général utilise `RawGameController.GetCurrentReading` et les dimensions fournies par Windows. Les noms d'entrées peuvent être absents ; les valeurs restent brutes jusqu'à leur identification.
- DualSense : le lecteur HID accepte l'USB et le Bluetooth, identifie la manette de manière prudente, lit le bouton microphone et, en Bluetooth, traite les rapports complets avec contrôle CRC. Il utilise des rapports d'activation Bluetooth sans effets lumineux. Une lecture HID périmée ne doit pas être présentée comme une entrée actuelle. Ne pas lire arbitrairement une autre manette du même modèle.
- Nintendo Switch Pro USB : `NintendoProControllerReader` cible `057E:2009`, le format observé 18 boutons / 4 axes / 1 switch, ouvre un périphérique HID correspondant et effectue une initialisation USB ainsi que la sélection du rapport complet `0x30`. Les sticks sont décodés en 12 bits ; les boutons et la croix sont séparés des données de temps et de mouvement. Les rapports de confirmation et incomplets ne sont pas interprétés comme des commandes. Le lecteur rend la croix via le switch pour éviter une double détection et utilise le format de profil `NintendoProUsbV1`. Il ne faut pas revenir silencieusement aux valeurs Windows erronées si cette lecture échoue.
- Nintendo : les essais synthétiques du parseur ne démontrent pas que l'initialisation ou la configuration fonctionnent sur une vraie manette. La dernière recherche Windows locale n'a pas vu de périphérique `057E:2009` connecté ; le diagnostic matériel reste ouvert. Les causes possibles de non-énumération doivent être diagnostiquées, sans installer de pilote tiers au hasard.
- **Défaut connu à vérifier avant de proposer un test complet Nintendo** : `BeginButtonTestButton_Click` dans `ControllerSuggestionWindow.Buttons.cs` appelle encore `ControllerAutoDetectionService.Detect`, qui rejette explicitement la Switch Pro USB. Même si la lecture native et la calibration fonctionnent, le test des boutons peut donc être bloqué. Corriger ce chemin avec la même source d'entrées que le reste de l'assistant avant de déclarer la fonctionnalité terminée ; conserver l'identité du format et tester aussi les autres manettes.

## Parcours « Suggérer la manette »

1. La configuration demande une connexion filaire. L'utilisateur sélectionne un modèle non déjà validé, ou un modèle personnalisé. Ce choix sert à nommer le profil et à choisir les commandes et le dessin ; il ne change pas automatiquement la compatibilité.
2. `ControllerConfigurationDraft` conserve en mémoire le nom, VID/PID et les résultats de la session. Une configuration inachevée et annulée est abandonnée ; ne pas supprimer un profil déjà enregistré par l'utilisateur.
3. La détection automatique affiche un chargement puis les informations et valeurs en direct. La lecture doit gérer déconnexion, annulation et erreur sans crash.
4. Une page distincte guide la calibration : centre puis X droite, X gauche, Y haut et Y bas pour chacun des deux sticks. Le retour visuel est approximatif ; les axes calibrés déterminent la normalisation. Les gâchettes ne sont pas recentrées comme des sticks. Préserver la fréquence de rafraîchissement et les instructions à l'écran.
5. Une page affiche la silhouette et les commandes propres au modèle : boutons principaux, croix, gâchettes, L3/R3 et commandes supplémentaires. L'utilisateur maintient chaque commande ; la capture stable est automatique, exige le relâchement entre deux commandes et refuse les affectations en double. Les commandes réellement indisponibles peuvent être signalées comme telles. Après capture, le schéma affiche les appuis en direct.
6. L'utilisateur peut enregistrer le profil local et envoyer le diagnostic indépendamment. L'envoi est explicite, et une fenêtre ou un navigateur ne doit pas être ouvert automatiquement.

`JoystickAxisConfiguration` enregistre `AxisIndex`, `Center`, `PositiveEnd`, `NegativeEnd` et normalise dans `[-1,1]`. La disposition peut être inversée matériellement ou visuellement ; ne pas supposer que toutes les manettes utilisent les mêmes indices ou le même sens d'axe.

## Profils locaux et données utilisateur

- Profils : `Documents/InputOS/profils/InputOS-VID-PID.json`, avec VID/PID hexadécimaux sur quatre caractères. Le chemin `Documents` est obtenu via `Environment.SpecialFolder.MyDocuments` et peut être redirigé.
- Paramètres : `%APPDATA%/InputOS/settings.json`, via `SettingsService`. `CloseToTray` vaut `true` par défaut. En cas de lecture impossible, les valeurs par défaut sont utilisées.
- Le profil JSON comprend notamment `SchemaVersion`, `ProfileType`, `ValidatedByInputOS`, `Model`, `VendorId`, `ProductId`, `Connection`, `InputReadingFormat`, les nombres d'entrées, `Joysticks` et `Buttons` avec leurs mappings et commandes indisponibles. `InputReadingFormat` vaut par défaut `WindowsRaw` pour les anciens profils ; `NintendoProUsbV1` identifie la lecture Nintendo spécifique. Vérifier la compatibilité du service distant avant de modifier le schéma envoyé.
- Les profils locaux sont provisoires et chargés uniquement en USB. La validation contrôle notamment type/version, indicateur de validation, VID/PID, connexion, dimensions, valeurs finies bornées, quatre axes distincts, sens de calibration, identifiants uniques et types/indices de commandes. Le profil Windows brut Nintendo historique ne doit pas être utilisé comme profil Nintendo HID.
- L'enregistrement utilise un fichier temporaire puis un remplacement ; conserver les données utilisateur et les fichiers déjà créés. Ne pas nettoyer `Documents/InputOS` ou `%APPDATA%/InputOS` lors d'une migration ou d'un test.
- Le testeur utilise un profil officiel applicable avant un profil local. Il doit exposer les entrées mappées, y compris la croix, les gâchettes, les clics de sticks et les commandes explicitement indisponibles.

## Diagnostics et confidentialité

Le bouton « Envoyer le diagnostic » appelle `ControllerDiagnosticService.SendAsync`. L'adresse HTTPS provient de `Config/diagnostic-settings.json`, copié au build et à la publication comme `diagnostic-settings.json`. Le service configuré au moment de cette rédaction est `https://inputos-diagnostics.guewenolivier.workers.dev/diagnostics`.

L'application envoie le JSON du profil par POST HTTPS, sans redirection automatique, avec délai de 25 secondes et annulation. Elle traite notamment les erreurs réseau, `429` et exige un `issueNumber` positif en réponse. Le flux existant vise la création d'une issue GitHub publique : communiquer clairement ce caractère public dans l'interface. La sauvegarde locale reste indépendante de l'envoi.

Le JSON doit contenir les informations de configuration nécessaires, pas le nom de compte Windows, le chemin de périphérique ou un identifiant privé. Ne pas mettre de token GitHub ou secret KV dans le client, le dépôt ou une issue. Les fonctions de construction d'URL d'issue préremplie existent encore dans le service d'export, mais le parcours actuel utilise l'envoi direct au Worker. Ne pas tester en envoyant de vraies issues sans autorisation explicite ; préférer des tests locaux simulés. Les sources du Worker n'étant pas présentes localement, ne pas affirmer que ses validations ou limites ont été vérifiées à partir du code actuel.

## Zone de notification, batterie et paramètres

`MainWindow` gère l'icône et le menu WinForms, le double-clic pour rouvrir, les actions ouvrir/quitter, le nombre de manettes et la manette sélectionnée. La fermeture peut réduire l'application dans la zone de notification selon `CloseToTray`. Préserver la désinscription des événements et la libération de l'icône à la fermeture effective.

La batterie intégrée concerne la DualShock 4 : pourcentage approximatif, charge et états associés lorsque disponibles. Ne pas annoncer le support batterie d'autres modèles sans implémentation et validation. Les fenêtres de paramètres gèrent aussi l'état et les actions des mises à jour.

## Mises à jour automatiques

`ApplicationUpdateService` utilise Velopack et les releases GitHub stables de `https://github.com/SomnyFR/InputOS` ; les préreleases ne sont pas sélectionnées par la source actuelle. La recherche est lancée au démarrage puis toutes les quatre heures. Une mise à jour disponible est téléchargée en arrière-plan ; elle est appliquée au lancement suivant ou au redémarrage demandé par l'utilisateur depuis les paramètres. Les erreurs réseau ne doivent pas empêcher l'application de fonctionner.

Les mises à jour nécessitent une installation Velopack ; une copie de développement, une installation ancienne ou la version portable ne suit pas automatiquement ce parcours. Un simple push ou le seul fichier `InputOS-Setup-v<version>.exe` ne constitue pas le flux Velopack : les index et paquets associés d'une release stable sont nécessaires. Ne pas changer arbitrairement l'identité du paquet, le canal ou les noms des fichiers référencés.

## Compilation de développement

Exécuter depuis la racine du projet dans PowerShell :

```powershell
dotnet --info
dotnet restore .\InputOS.csproj
dotnet build .\InputOS.csproj -c Debug --no-restore
dotnet run --project .\InputOS.csproj -c Debug --no-build --no-restore
```

Le programme de développement se trouve normalement dans `bin\Debug\net10.0-windows10.0.19041.0\InputOS.exe`. Pour vérifier la configuration Release :

```powershell
dotnet build .\InputOS.csproj -c Release --no-restore
```

N'utiliser `--no-restore` que si les dépendances nécessaires sont déjà restaurées, et `--no-build` que si le binaire est à jour. La restauration peut nécessiter l'accès aux sources NuGet et à la configuration locale. Signaler une restauration ou une compilation bloquée au lieu d'affirmer qu'elle a réussi. Ne pas lancer l'application installée ou modifier son installation pour une simple vérification de build.

## Publication autonome et fabrication d'une release

Une publication .NET autonome n'est pas à elle seule un paquet de mise à jour Velopack :

```powershell
dotnet publish .\InputOS.csproj -c Release -r win-x64 --self-contained true -o .\obj\VelopackPublish\1.1.1 -p:Version=1.1.1
```

Remplacer `1.1.1` par la version explicitement préparée et maintenir cohérents projet, README, nom de version, dossier et métadonnées. Pour une nouvelle release, utiliser les scripts locaux si présents :

```powershell
.\Packaging\Build-Release.ps1 -Version 1.1.1 -PreviousReleaseDirectory .\Releases\1.1.0
.\Packaging\Build-MigrationSetup.ps1 -Version 1.1.1
```

`-PreviousReleaseDirectory` est optionnel ; il copie les anciens `*-full.nupkg` et `releases.win.json`, permettant notamment la création d'un delta. La version du script est `major.minor.patch`, sans `v`. Le script de release installe si nécessaire l'outil local `vpk` 1.2.0 dans `obj/VelopackTools`, publie pour `win-x64` dans `obj/VelopackPublish/<version>`, puis construit les artefacts dans `Releases/<version>` avec `packId` et titre `InputOS`, canal `win`, exécutable `InputOS.exe` et icône `iOSBlack.ico`. Il passe `-p:NuGetAudit=false` : l'audit de vulnérabilités NuGet est désactivé, ce n'est pas un contrôle de sécurité réussi.

Le dossier de release n'est pas automatiquement nettoyé. Vérifier les fichiers existants, leurs versions et leurs index ; ne pas supprimer les paquets historiques référencés. Les scripts ne publient rien sur GitHub et ne lancent pas le programme d'installation.

### Setup de migration Inno Setup

`Packaging/Build-MigrationSetup.ps1` exige d'abord `Releases/<version>/InputOS-win-Setup.exe`. Il cherche `ISCC.exe` dans `obj/InnoSetup`, puis dans les emplacements Inno Setup 6/7 usuels ; un chemin explicite peut être fourni :

```powershell
.\Packaging\Build-MigrationSetup.ps1 -Version 1.1.1 -CompilerPath 'C:\chemin\ISCC.exe'
```

Le script compile `Packaging/InputOS-Migration.iss` et produit `InputOS-Setup-v<version>.exe`. Le wrapper requiert les privilèges administrateur, cherche l'installation historique dans les registres HKLM/HKCU 32/64 bits, reconnaît notamment `InputOS` et `InputOS version ...`, et propose la migration. L'identifiant historique à conserver est `{F4634D47-B96D-4D68-A0A6-68C719120E49}`. Le désinstalleur trouvé est vérifié avant exécution ; le wrapper appelle ensuite le setup Velopack avec le contexte de l'utilisateur d'origine. Préserver paramètres et profils. Compiler le setup ne justifie pas d'exécuter une installation ou une désinstallation sur le PC de l'utilisateur.

### Fichiers à vérifier et à publier sur GitHub

L'exemple historique `Releases/1.1.1` contient :

```text
assets.win.json
InputOS-1.1.0-full.nupkg
InputOS-1.1.1-delta.nupkg
InputOS-1.1.1-full.nupkg
InputOS-Setup-v1.1.1.exe
InputOS-win-Portable.zip
InputOS-win-Setup.exe
RELEASES
releases.win.json
```

Cette liste est un exemple réel, pas la garantie que toutes les futures versions produiront les mêmes noms de paquets historiques. Avant une release, contrôler la liste réellement générée et publier tous les fichiers nécessaires du dossier, sans renommer les paquets/index référencés. Comparer noms, tailles et empreintes des fichiers avec les index ; vérifier version, icônes et `diagnostic-settings.json`.

```powershell
Get-ChildItem .\Releases\1.1.1 -File | Select-Object Name,Length
Get-FileHash .\Releases\1.1.1\InputOS-1.1.1-full.nupkg -Algorithm SHA256
```

Le wrapper de migration n'est pas nécessairement répertorié dans l'index Velopack. Les fichiers historiques 1.1.1 présents localement ne prouvent pas que les modifications Nintendo ultérieures sont incluses : une compilation et un contrôle des artefacts sont nécessaires. Publier ou modifier une release distante uniquement à la demande autorisée de l'utilisateur.

## Vérifications et tests

Aucun projet de tests standard suivi n'a été découvert lors de la rédaction. Des aides locales peuvent exister dans `obj/` : `ButtonCheck`, `ButtonPageQa`, `DetectionUiCheck`, `DualSenseCheck`, `ExportCheck`, `JoystickCheck`, `JoystickPageQa`, `MicrophoneCheck`, `NintendoCheck`, `UpdaterCheck`, `WorkerCheck` et `XboxSeriesCheck`. Ces dossiers sont ignorés, peuvent être absents d'un clone ou périmés et ne constituent pas une CI garantie. Vérifier leur présence et leur contenu avant utilisation ; `WorkerCheck` peut notamment dépendre de sources Cloudflare absentes.

Exemples, si les projets et dépendances restaurées existent :

```powershell
dotnet run --project .\obj\NintendoCheck\Check.csproj --no-restore -p:NuGetAudit=false
dotnet run --project .\obj\DualSenseCheck\Check.csproj --no-restore -p:NuGetAudit=false
dotnet run --project .\obj\XboxSeriesCheck\Check.csproj --no-restore -p:NuGetAudit=false
```

Adapter les contrôles au changement : compilation WPF, lecture/calibration et repos/appui, diagonales et doubles affectations, sauvegarde/validation du JSON, confidentialité, disponibilité des commandes, capture en cas de déconnexion, rendu des schémas, tray et mises à jour. Les aides peuvent tester les octets HID synthétiques ; elles ne remplacent pas les essais sur une manette physique pour USB et Bluetooth. Ne jamais affirmer un test matériel non réalisé.

Pour une release, vérifier la compilation, la cohérence des versions, le contenu et les empreintes des paquets/index, ainsi que la génération du setup. Un test de migration ou de mise à jour réelle doit utiliser un environnement de test approprié et préserver les installations et données utilisateur.

## État et pistes ouvertes

- `README.md` décrit actuellement la version 1.1.1. La « Détection Manuelle » a été évoquée pour 1.1.2 ; ne pas la présenter comme déjà disponible ni modifier la version sans demande.
- Les changements Nintendo présents localement sont en cours ; vérifier `git status` et préserver tout travail non commité. Le blocage connu du démarrage du test des boutons et l'absence de validation matérielle sont décrits plus haut.
- Si Windows ne détecte pas du tout une manette USB, commencer par l'énumération Windows, le câble de données et le port. Un lecteur propre à InputOS ne peut pas décoder un périphérique qui n'est pas présenté à l'application ; ne pas confondre le problème de connexion avec un mapping incorrect.
- Adapter ce guide lors de toute évolution : version, organisation, profils, transport, formats, procédure de compilation et publication, puis distinguer les faits vérifiés des travaux prévus.
