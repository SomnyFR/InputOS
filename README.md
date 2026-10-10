# InputOS

InputOS is a Windows application designed to detect, identify, configure, and test controllers.

It provides real-time input monitoring, local controller profiles, and a guided workflow to help add support for new controller models.

---

## 🎮 Current Compatibility

### Official Profiles

- DualShock 4
- DualSense Edge
- Xbox Elite Series 2

### Controllers Without an Official Profile

DualSense and other controllers detected by Windows can be configured through the controller suggestion workflow.

A compatible local profile enables testing over USB while official support is pending.

> Detection does not guarantee full compatibility. Support and axis mapping depend on the exact controller model and hardware revision.

---

## ✨ New in v1.1.0

### Guided Controller Configuration

When a controller has no compatible profile, InputOS displays a warning and a **Suggest Controller** button.

The guided workflow includes:

- Controller model selection.
- Automatic detection of available buttons, axes, and switches.
- Visual joystick calibration: center, right, left, up, and down for each stick.
- Automatic button mapping without confirming every button.
- Prevention of duplicate input assignments.
- Trigger and stick-click mapping, including L3/R3.
- DualSense microphone button detection.
- A controller diagram adapted to the selected model.
- Live feedback for mapped inputs.

**A wired USB connection is required for configuration and use of local profiles.**

Values displayed during configuration are approximate. Their precision will be refined when developers validate the official profile.

Canceling an unfinished configuration discards its temporary configuration data.

### Local Controller Profiles

Use **Save** to store your configured profile automatically in:

```text
Documents\InputOS\profils
```

InputOS loads compatible local profiles so you can use the controller tester while official support is pending.

When a future update provides an official profile, that profile takes priority.

### Diagnostic Submission

Use **Send Diagnostic** to submit your controller configuration directly from InputOS.

- No browser needs to be opened.
- No manual GitHub issue creation is required.
- The diagnostic service creates a public GitHub issue for developer review.
- Diagnostics include controller identifiers and input mappings.
- Windows device names and device paths are excluded.

### Automatic Updates

- Update checks at startup and periodically while InputOS is running.
- Background downloads.
- Downloaded updates applied on the next launch.
- Update controls available in Settings.

> Automatic updates require the installed version. The portable version does not use this update workflow.

### Installer Improvements

- Detection of the previous v1.0.0 installation.
- Option to remove the previous installation during migration.
- Preservation of settings and saved profiles.
- Corrected application and installer icons.

---

## 🧰 Features

### Controller Detection

InputOS detects controllers exposed by Windows and displays:

- Controller name
- VID
- PID
- Button count
- Axis count
- Switch count

### Controller Tester

The tester monitors inputs in real time:

- Buttons
- Left joystick
- Right joystick
- L2 / LT
- R2 / RT
- D-Pad
- Raw axis values

Each joystick includes an option to visually invert its Y axis independently.

The tester requires an official profile or a compatible local profile.

### Battery Information

Battery monitoring is currently supported for compatible DualShock 4 controllers.

InputOS can display:

- Approximate battery percentage
- Charging state
- Fully charged state
- Certain charging errors

Battery support for additional controllers may be added in future versions.

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
- A controller detected by Windows
- A wired USB connection for controller configuration and local profiles
- An internet connection for diagnostic submission and update downloads

The installer includes the components required to run InputOS.

---

## 📦 Installation

Download the latest version from the repository’s **Releases** section.

For v1.1.0, run:

```text
InputOS-Setup-v1.1.0.exe
```

### Upgrading from v1.0.0

1. Close InputOS using its system tray icon.
2. Run `InputOS-Setup-v1.1.0.exe`.
3. Follow the migration instructions.

The installer detects the previous installation and offers to remove it before installing v1.1.0 with automatic updates.

Your settings and profiles in `Documents\InputOS\profils` are preserved.

---

## 🗂️ Project Structure

- `Views/Windows/` — Main windows and controller tester.
- `Views/Windows/ControllerSuggestion/` — Guided controller configuration.
- `Models/` — Data models and profiles.
- `Services/Controllers/` — Detection, calibration, profiles, and diagnostics.
- `Services/Settings/` — Application settings.
- `Services/Updates/` — Application updates.
- `Config/` — Diagnostic service configuration.
- `Assets/` — Icons and images.
- `Properties/` — Assembly information and shared imports.
- `Cloudflare/` — Diagnostic backend and deployment guide.
- `Packaging/` — Release and migration installer scripts.
- `Packaging/Legacy/` — Original v1 installer script, kept for reference.
- `Releases/<version>/` — Generated installers and packages, excluded from Git.
- `bin/` and `obj/` — Build outputs. The previous publication is archived in `obj/LegacyPublish/`.

The entry points `App.xaml` and `Program.cs` remain at the project root alongside `InputOS.csproj`.

`Config/diagnostic-settings.json` is copied next to the executable as `diagnostic-settings.json`.
