using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class XboxSeriesProfileService
{
    // ============================================================================
    // FONCTIONNALITÉ : PROFIL XBOX SERIES USB — DIAGNOSTICS GITHUB #4 ET #6
    // ============================================================================

    public static bool IsSupported(ushort vendor, ushort product, bool wireless,
        int buttons, int axes, int switches) =>
        vendor == 0x045E && product == 0x0B12 && !wireless &&
        buttons == 14 && axes == 6 && switches == 0;

    public static ControllerLocalProfile? Load(RawGameController controller) =>
        ControllerCompatibilityService.HasValidatedProfile(controller.HardwareVendorId, controller.HardwareProductId) &&
        IsSupported(controller.HardwareVendorId, controller.HardwareProductId, controller.IsWireless,
            controller.ButtonCount, controller.AxisCount, controller.SwitchCount) ? CreateProfile() : null;

    public static ControllerLocalProfile CreateProfile() => new(
        1, "OfficialProfile", true, "Xbox Series S/X", 0x045E, 0x0B12, "USB", 14, 6, 0,
        // Centres et amplitudes nominaux : ne pas généraliser la calibration d'un exemplaire.
        new(new(1, .5, 1, 0), new(0, .5, 1, 0), new(3, .5, 1, 0), new(2, .5, 1, 0)),
        new([
            new("FaceBottom", "A", "Button", 0),
            new("FaceRight", "B", "Button", 1),
            new("FaceLeft", "X", "Button", 2),
            new("FaceTop", "Y", "Button", 3),
            new("DpadUp", "Croix directionnelle : haut", "Button", 10),
            new("DpadDown", "Croix directionnelle : bas", "Button", 12),
            new("DpadLeft", "Croix directionnelle : gauche", "Button", 13),
            new("DpadRight", "Croix directionnelle : droite", "Button", 11),
            new("LeftShoulder", "LB", "Button", 4),
            new("RightShoulder", "RB", "Button", 5),
            new("LeftTrigger", "LT", "Axis", 4, RestValue: 0, PressedValue: 1),
            new("RightTrigger", "RT", "Axis", 5, RestValue: 0, PressedValue: 1),
            new("LeftStick", "L3 — clic du joystick gauche", "Button", 8),
            new("RightStick", "R3 — clic du joystick droit", "Button", 9),
            new("MenuLeft", "View", "Button", 6),
            new("MenuRight", "Menu", "Button", 7)
        ], ["Xbox / Guide", "Share"]));

    // ============================================================================
    // FIN FONCTIONNALITÉ : PROFIL XBOX SERIES USB
    // ============================================================================
}
