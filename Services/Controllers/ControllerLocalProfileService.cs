using System.IO;
using System.Text.Json;
using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class ControllerLocalProfileService
{
    // ============================================================================
    // FONCTIONNALITÉ : UTILISATION PROVISOIRE DES PROFILS LOCAUX USB
    // ============================================================================
    public static ControllerLocalProfile? Load(RawGameController controller)
    {
        if (controller.IsWireless || ControllerCompatibilityService.HasValidatedProfile(controller)) return null;
        try
        {
            string path = Path.Combine(ControllerProfileExportService.GetProfilesDirectory(),
                $"InputOS-{controller.HardwareVendorId:X4}-{controller.HardwareProductId:X4}.json");
            if (!File.Exists(path) || new FileInfo(path).Length > 65536) return null;
            var profile = JsonSerializer.Deserialize<ControllerLocalProfile>(File.ReadAllText(path));
            if (profile == null || !IsValid(profile, controller.HardwareVendorId, controller.HardwareProductId,
                controller.ButtonCount, controller.AxisCount, controller.SwitchCount)) return null;
            return profile;
        }
        catch (Exception) { return null; }
    }

    public static bool IsValid(ControllerLocalProfile p, ushort vendor, ushort product, int buttons, int axes, int switches)
    {
        if (p.SchemaVersion != 1 || p.ProfileType != "UserConfiguration" || p.ValidatedByInputOS ||
            string.IsNullOrWhiteSpace(p.Model) || p.Model.Length > 100 || p.Connection != "USB" ||
            p.VendorId != vendor || p.ProductId != product || p.ButtonCount != buttons ||
            p.AxisCount != axes || p.SwitchCount != switches || p.Joysticks == null ||
            p.Buttons?.Mappings == null || p.Buttons.UnavailableButtons == null || p.Buttons.Mappings.Length > 64) return false;
        var sticks = new[] { p.Joysticks.LeftX, p.Joysticks.LeftY, p.Joysticks.RightX, p.Joysticks.RightY };
        if (sticks.Any(a => a == null || a.AxisIndex < 0 || a.AxisIndex >= axes ||
            !Unit(a.Center) || !Unit(a.PositiveEnd) || !Unit(a.NegativeEnd) ||
            (a.PositiveEnd - a.Center) * (a.NegativeEnd - a.Center) >= 0) ||
            sticks.Select(a => a.AxisIndex).Distinct().Count() != 4) return false;
        if (p.Buttons.Mappings.Any(m => m == null || string.IsNullOrWhiteSpace(m.Id) ||
            string.IsNullOrWhiteSpace(m.Name)) ||
            p.Buttons.Mappings.Select(m => m.Id).Distinct().Count() != p.Buttons.Mappings.Length) return false;
        return p.Buttons.Mappings.All(m => m.InputType switch
        {
            "Button" => m.InputIndex >= 0 && m.InputIndex < buttons,
            "Axis" => m.InputIndex >= 0 && m.InputIndex < axes && !sticks.Any(a => a.AxisIndex == m.InputIndex) &&
                m.RestValue is double rest && m.PressedValue is double pressed && Unit(rest) && Unit(pressed) && Math.Abs(pressed - rest) > .01,
            "Switch" => m.InputIndex >= 0 && m.InputIndex < switches && m.SwitchPosition is "Up" or "Down" or "Left" or "Right",
            "HidButton" => vendor == 0x054C && product is 0x0CE6 or 0x0DF2 && m.Id == "Microphone" && m.InputIndex == 10 && m.SwitchPosition == "Bit2",
            _ => false
        });
    }

    private static bool Unit(double value) => double.IsFinite(value) && value >= 0 && value <= 1;
    // ============================================================================
    // FIN FONCTIONNALITÉ : UTILISATION PROVISOIRE DES PROFILS LOCAUX USB
    // ============================================================================
}
