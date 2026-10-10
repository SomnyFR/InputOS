# InputOS

## Organisation du projet

- `Views/Windows/` : fenêtres principales et testeur.
- `Views/Windows/ControllerSuggestion/` : parcours de configuration des manettes.
- `Models/` : données et profils.
- `Services/Controllers/` : détection, calibration, profils et diagnostics.
- `Services/Settings/` et `Services/Updates/` : paramètres et mises à jour.
- `Config/` : configuration du service de diagnostic.
- `Assets/` : icônes et images.
- `Properties/` : informations d’assembly et imports communs.
- `Cloudflare/` : service de réception des diagnostics et guide d’installation.
- `Packaging/` : scripts pour créer les versions et le setup de migration.
- `Packaging/Legacy/` : ancien script de la v1, conservé comme référence.
- `Releases/<version>/` : setups et paquets générés, exclus de Git.
- `bin/` et `obj/` : fichiers de compilation ; l’ancienne publication est archivée dans `obj/LegacyPublish/`.

Les points d’entrée `App.xaml` et `Program.cs` restent à la racine avec `InputOS.csproj`.
Le fichier `Config/diagnostic-settings.json` est copié à côté de l’exécutable sous le nom `diagnostic-settings.json`.

InputOS is a Windows application designed to detect, identify, and test PlayStation and Xbox controllers.

The project aims to provide a simple interface for checking controller inputs and, over time, centralize more controller configuration features.

---

## 🎮 Current Compatibility

### PlayStation

- DualShock 4
- DualSense Edge

### Xbox

- Xbox Elite Series 2

### Controllers Without an Official Profile

- DualSense and other controllers detected by Windows can be configured through the guided controller suggestion workflow.
- Xbox One / Series controller detection does not guarantee a validated profile for every model.
- A compatible local profile enables testing over USB while official support is pending.

> Compatibility and axis mapping may vary depending on the exact controller model and hardware revision.

---

## ✨ Features

### New in v1.1.0 — Guided Controller Configuration

When a controller has no compatible profile, InputOS displays a warning and a **Suggest Controller** button.

The guided workflow includes:

- Controller model selection.
- Automatic detection of available buttons, axes, and switches.
- Visual joystick calibration: center, right, left, up, and down for each stick.
- Automatic button mapping without confirming every button, with duplicate assignments prevented.
- Trigger and stick-click mapping, including L3/R3.
- DualSense microphone button detection.
- A controller diagram adapted to the selected model and live feedback for mapped inputs.

**A wired USB connection is required for configuration and use of local profiles.** Displayed values during configuration are approximate and will be refined when the official profile is validated by the developers.

### Local Profiles

Use **Save** to store your configured profile automatically in:

```text
Documents\InputOS\profils
```

InputOS loads compatible local profiles so you can use the controller tester while official support is pending. When a future update provides an official profile, that profile takes priority.

Canceling an unfinished configuration discards its temporary configuration data.

### Diagnostic Submission

Use **Send Diagnostic** to submit the controller configuration directly from InputOS. No browser or manual GitHub issue creation is required.

The diagnostic service creates a public GitHub issue to help developers review the proposed profile. Diagnostics contain controller identifiers and input mappings; Windows device names and device paths are excluded.

### Automatic Updates

- Checks for updates at startup and periodically while InputOS is running.
- Downloads available updates in the background.
- Applies downloaded updates on the next launch.
- Provides update controls in Settings.

Automatic updates require the installed version. The portable version does not use this update workflow.

---

### Controller Detection

InputOS automatically detects connected controllers and displays information such as:

- Controller name
- VID
- PID
- Button count
- Axis count
- Switch count

---

### Controller Tester

The controller tester allows you to monitor inputs in real time:

- Buttons
- Left joystick
- Right joystick
- L2 / LT
- R2 / RT
- D-Pad
- Raw axis values

Each joystick also includes an option to visually invert its Y axis independently.

---

### Battery Information

Battery information is currently supported for:

- DualShock 4

InputOS can display:

- Approximate battery percentage
- Charging state
- Fully charged state
- Certain charging errors

Battery support for additional controllers may be added in future versions.

---

### System Tray

InputOS can continue running in the Windows system tray.

Available features include:

- Minimize to background
- Double-click the tray icon to reopen InputOS
- Right-click tray menu
- Open InputOS
- Quit InputOS
- Display the selected controller
- Display the number of detected controllers

---

## 🖥️ Requirements

- Windows 10 or Windows 11
- 64-bit system
- USB or another compatible controller connection

The InputOS installer includes the components required to run the application.

---

## 📦 Installation

Download the latest version from the:

**Releases**

section of this repository.

Then run:

```text
InputOS-Setup-v1.1.0.exe
```

### Upgrading from v1.0.0

Close InputOS using its system tray icon before running `InputOS-Setup-v1.1.0.exe`.

The migration installer detects the previous installation and offers to remove it before installing v1.1.0 with automatic updates. Your settings and profiles in `Documents\InputOS\profils` are preserved.

Application and installer icons have also been corrected in v1.1.0.
