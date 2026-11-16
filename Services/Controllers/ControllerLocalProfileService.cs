using System.IO;
using System.Text.Json;
using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class ControllerLocalProfileService
{
    // ============================================================================
    // FONCTIONNALITÉ : UTILISATION PROVISOIRE DES PROFILS LOCAUX
    // ============================================================================
    public const int MaxButtons = 128, MaxAxes = 32, MaxSwitches = 8;
    private static readonly string[] AxisTypes = ["Stick", "Trigger", "Throttle", "Slider", "Dial"];

    public static readonly string[] ControllerTypes = ["Gamepad", "ArcadeStick", "Wheel", "Joystick", "RadioController", "Other"];

    // Le service de diagnostics refuse ces caractères et les noms de plus de 100 caractères.
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 100 && name.IndexOfAny(['`', '\uFF20', '<', '>', '\r', '\n']) < 0;

    // Connexion réelle d'après Windows. shortcut: RawGameController n'expose pas le transport ;
    // un récepteur 2,4 GHz n'est reconnu que par son nom, sinon une manette sans fil est « Bluetooth ».
    public static string GetConnection(RawGameController controller)
    {
        if (!controller.IsWireless) return "USB";
        string name = controller.DisplayName ?? "";
        return new[] { "2.4", "dongle", "receiver", "récepteur", "adapter" }
            .Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)) ? "Wireless" : "Bluetooth";
    }

    // Nom de fichier avec la connexion : une même manette a une disposition différente en USB et en Bluetooth.
    public static string GetProfileFileName(ushort vendor, ushort product, string connection) =>
        $"InputOS-{vendor:X4}-{product:X4}-{connection}.json";

    public static ControllerLocalProfile? Load(RawGameController controller)
    {
        try
        {
            string connection = GetConnection(controller);
            string directory = ControllerProfileExportService.GetProfilesDirectory();
            string path = Path.Combine(directory,
                GetProfileFileName(controller.HardwareVendorId, controller.HardwareProductId, connection));
            // Anciens fichiers sans connexion : lus comme USB.
            string legacy = Path.Combine(directory, $"InputOS-{controller.HardwareVendorId:X4}-{controller.HardwareProductId:X4}.json");
            if (ControllerCompatibilityService.HasValidatedProfile(controller))
            {
                // Un profil officiel qui s'applique à cette manette (VID/PID, transport et nombres d'entrées)
                // remplace le profil local de même connexion. On archive au lieu de supprimer.
                foreach (string file in connection == "USB" ? new[] { path, legacy } : new[] { path })
                    if (File.Exists(file))
                    {
                        string archive = Path.Combine(directory, "archive");
                        Directory.CreateDirectory(archive);
                        File.Move(file, Path.Combine(archive, Path.GetFileName(file)), true);
                    }
                return null;
            }
            if (!File.Exists(path) && connection == "USB") path = legacy;
            if (!File.Exists(path) || new FileInfo(path).Length > 262144) return null;
            var profile = JsonSerializer.Deserialize<ControllerLocalProfile>(File.ReadAllText(path));
            if (profile == null || !IsValid(profile, controller.HardwareVendorId, controller.HardwareProductId,
                controller.ButtonCount, controller.AxisCount, controller.SwitchCount, connection)) return null;
            return profile;
        }
        catch (Exception) { return null; }
    }

    public static bool IsValid(ControllerLocalProfile p, ushort vendor, ushort product, int buttons, int axes, int switches,
        string connection = "USB")
    {
        // Un profil issu des anciennes entrées Windows ne peut pas être utilisé avec le décodage Nintendo.
        if (p.InputReadingFormat != (vendor == 0x057E && product == 0x2009 && connection == "USB"
            ? NintendoProControllerReader.ReadingFormat : "WindowsRaw")) return false;
        bool v1 = p.SchemaVersion == 1;
        if (!(v1 || p.SchemaVersion == 2) || p.ProfileType != "UserConfiguration" || p.ValidatedByInputOS ||
            string.IsNullOrWhiteSpace(p.Model) || p.Model.Length > 100 ||
            p.Connection != connection || p.Connection is not ("USB" or "Bluetooth" or "Wireless") ||
            !ControllerTypes.Contains(p.ControllerType) ||
            p.VendorId != vendor || p.ProductId != product || p.ButtonCount != buttons ||
            p.AxisCount != axes || p.SwitchCount != switches || p.Joysticks == null ||
            buttons < 0 || buttons > MaxButtons || axes < 0 || axes > MaxAxes || switches < 0 || switches > MaxSwitches ||
            p.Buttons?.Mappings == null || p.Buttons.UnavailableButtons == null ||
            p.Buttons.Mappings.Length > MaxButtons + MaxAxes + 4 * MaxSwitches || p.Buttons.UnavailableButtons.Length > 128) return false;
        var j = p.Joysticks;
        if (j.Axes == null) return false;
        // v1 : toujours un gamepad à deux sticks, sans autre axe.
        if (v1 && (p.Connection != "USB" || p.ControllerType != "Gamepad" || j.Axes.Length != 0 ||
            j.StickAxes().Length != 4)) return false;
        if ((j.LeftX == null) != (j.LeftY == null) || (j.RightX == null) != (j.RightY == null)) return false;
        var all = j.AllAxes();
        if (all.Length > axes || all.Any(a => a == null || a.AxisIndex < 0 || a.AxisIndex >= axes ||
            !Unit(a.Center) || !Unit(a.PositiveEnd) || !Unit(a.NegativeEnd) || !AxisTypes.Contains(a.Type) ||
            Math.Abs(a.PositiveEnd - a.Center) <= .01 ||
            (a.Name != null && !IsValidName(a.Name)) || !double.IsFinite(a.Deadzone) || a.Deadzone < 0 || a.Deadzone > .5 ||
            (a.Type == "Stick" ? (a.PositiveEnd - a.Center) * (a.NegativeEnd - a.Center) >= 0 : a.PositiveEnd == a.NegativeEnd)) ||
            all.Select(a => a.AxisIndex).Distinct().Count() != all.Length ||
            j.StickAxes().Any(a => a.Type != "Stick") || j.Axes.Any(a => a.Type == "Stick")) return false;
        var sticks = j.StickAxes();
        if (p.Buttons.Mappings.Any(m => m == null || string.IsNullOrWhiteSpace(m.Id) || m.Id.Length > 64 ||
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
    // FIN FONCTIONNALITÉ : UTILISATION PROVISOIRE DES PROFILS LOCAUX
    // ============================================================================
}
