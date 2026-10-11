using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class DualSenseProfileService
{
    // ============================================================================
    // FONCTIONNALITÉ : PROFIL DUALSENSE USB — DIAGNOSTIC GITHUB #1
    // ============================================================================

    public static bool IsSupported(ushort vendor, ushort product, bool wireless,
        int buttons, int axes, int switches) =>
        vendor == 0x054C && product == 0x0CE6 &&
        (buttons == 15 || (wireless && buttons == 14)) && axes == 6 && switches == 1;

    public static ControllerLocalProfile? Load(RawGameController controller) =>
        ControllerCompatibilityService.HasValidatedProfile(controller.HardwareVendorId, controller.HardwareProductId) &&
        IsSupported(controller.HardwareVendorId, controller.HardwareProductId,
            controller.IsWireless, controller.ButtonCount, controller.AxisCount, controller.SwitchCount)
            ? CreateProfile() with { Connection = controller.IsWireless ? "Bluetooth" : "USB", ButtonCount = controller.ButtonCount } : null;

    public static ControllerLocalProfile CreateProfile() => new(
        1, "OfficialProfile", true, "DualSense", 0x054C, 0x0CE6, "USB", 15, 6, 1,
        // Centre nominal : les centres mesurés dans le diagnostic sont individuels.
        new(new(0, .5, 1, 0), new(1, .5, 0, 1), new(2, .5, 1, 0), new(5, .5, 0, 1)),
        new([
            new("FaceBottom", "Croix", "Button", 1),
            new("FaceRight", "Cercle", "Button", 2),
            new("FaceLeft", "Carré", "Button", 0),
            new("FaceTop", "Triangle", "Button", 3),
            new("DpadUp", "Croix directionnelle : haut", "Switch", 0, "Up"),
            new("DpadDown", "Croix directionnelle : bas", "Switch", 0, "Down"),
            new("DpadLeft", "Croix directionnelle : gauche", "Switch", 0, "Left"),
            new("DpadRight", "Croix directionnelle : droite", "Switch", 0, "Right"),
            new("LeftShoulder", "L1", "Button", 4),
            new("RightShoulder", "R1", "Button", 5),
            new("LeftTrigger", "L2", "Axis", 3, RestValue: 0, PressedValue: 1),
            new("RightTrigger", "R2", "Axis", 4, RestValue: 0, PressedValue: 1),
            new("LeftStick", "L3 — clic du joystick gauche", "Button", 10),
            new("RightStick", "R3 — clic du joystick droit", "Button", 11),
            new("MenuLeft", "Create", "Button", 8),
            new("MenuRight", "Options", "Button", 9),
            new("Home", "PS", "Button", 12),
            new("Touchpad", "Appui sur le pavé tactile", "Button", 13),
            new("Microphone", "Bouton microphone", "HidButton", 10, "Bit2")
        ], []));

    // ============================================================================
    // FIN FONCTIONNALITÉ : PROFIL DUALSENSE USB
    // ============================================================================
}
