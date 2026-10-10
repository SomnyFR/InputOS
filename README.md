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
- DualSense
- DualSense Edge

### Xbox

- Xbox Elite Series 2
- Detection of Xbox One / Series controllers

> Compatibility and axis mapping may vary depending on the exact controller model and hardware revision.

---

## ✨ Features

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
InputOS-Setup-v1.0.0.exe
