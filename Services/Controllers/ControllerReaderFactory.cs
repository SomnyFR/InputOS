using static InputOS.Services.LocalizationService;
using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

// ============================================================================
// FONCTIONNALITÉ : CHOIX CENTRAL DU LECTEUR D'ENTRÉES
// ============================================================================

public interface IControllerInputReader : IDisposable
{
    // Format de lecture écrit dans le profil (WindowsRaw, NintendoProUsbV1…).
    string InputReadingFormat { get; }

    // Faux si la dernière lecture est périmée : elle ne doit alors jamais être présentée comme actuelle.
    bool IsFresh { get; }

    // Dernière lecture fraîche, ou null si elle est périmée. Un lecteur Windows lève une exception si la manette est déconnectée.
    ControllerDetectionResult? Read();

    // Première lecture (bloquante, à appeler hors du thread de l'interface).
    ControllerDetectionResult WaitForReading(CancellationToken token);
}

public enum ControllerReaderKind { WindowsRaw, DualSense, NintendoProUsb }

// Lecture Windows brute : lecteur universel et secours des lecteurs spécifiques.
public sealed class WindowsRawControllerReader(RawGameController controller) : IControllerInputReader
{
    private ControllerDetectionResult? template;

    public string InputReadingFormat => "WindowsRaw";
    public bool IsFresh => RawGameController.RawGameControllers.Contains(controller);

    public ControllerDetectionResult? Read()
    {
        // Les noms des boutons ne changent pas : ils ne sont lus qu'une fois.
        if (template == null) return template = ControllerAutoDetectionService.Detect(controller);
        if (!IsFresh) throw new InvalidOperationException(Tr("La manette est déconnectée."));
        var buttons = new bool[controller.ButtonCount];
        var axes = new double[controller.AxisCount];
        var switches = new GameControllerSwitchPosition[controller.SwitchCount];
        controller.GetCurrentReading(buttons, switches, axes);
        return template with
        {
            ButtonStates = buttons,
            AxisValues = axes,
            SwitchPositions = switches.Select(position => position.ToString()).ToArray()
        };
    }

    public ControllerDetectionResult WaitForReading(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Read()!;
    }

    public void Dispose() { }
}

// DualSense : entrées Windows + bouton micro lu en HID (USB et Bluetooth).
public sealed class DualSenseControllerReader : IControllerInputReader
{
    private readonly WindowsRawControllerReader raw;
    private readonly DualSenseMicrophoneReader hid;

    public DualSenseControllerReader(RawGameController controller)
    {
        raw = new WindowsRawControllerReader(controller);
        hid = new DualSenseMicrophoneReader(controller);
    }

    public string InputReadingFormat => raw.InputReadingFormat;
    public bool IsFresh => raw.IsFresh;
    // Rapport HID complet (Bluetooth), null s'il est périmé.
    public ControllerDetectionResult? HidReading => hid.Reading;

    public ControllerDetectionResult? Read() => raw.Read() is { } reading ? reading with { MicrophonePressed = hid.Pressed } : null;
    public ControllerDetectionResult WaitForReading(CancellationToken token) => Read()!;
    public void Dispose() => hid.Dispose();
}

public static class ControllerReaderFactory
{
    // Sélection pure (testable sans manette). connection : « USB », « Bluetooth » ou « Wireless ».
    public static ControllerReaderKind Select(ushort vendorId, ushort productId, string connection) =>
        (vendorId, productId) switch
        {
            (0x054C, 0x0CE6 or 0x0DF2) => ControllerReaderKind.DualSense,
            // Switch Pro en Bluetooth : format HID inconnu, lecture Windows brute (à valider sur matériel).
            (0x057E, 0x2009) when connection == "USB" => ControllerReaderKind.NintendoProUsb,
            _ => ControllerReaderKind.WindowsRaw
        };

    public static ControllerReaderKind Select(RawGameController controller) =>
        Select(controller.HardwareVendorId, controller.HardwareProductId,
            ControllerLocalProfileService.GetConnection(controller));

    public static IControllerInputReader CreateRaw(RawGameController controller) => new WindowsRawControllerReader(controller);

    // Lecteur adapté au modèle ; si sa création échoue, lecture Windows brute et message dans warning.
    public static IControllerInputReader Create(RawGameController controller, out string? warning)
    {
        warning = null;
        try
        {
            switch (Select(controller))
            {
                case ControllerReaderKind.DualSense: return new DualSenseControllerReader(controller);
                case ControllerReaderKind.NintendoProUsb: return new NintendoProControllerReader(controller);
            }
        }
        catch (Exception)
        {
            warning = FallbackMessage;
        }
        return CreateRaw(controller);
    }

    public static string FallbackMessage =>
        Tr("Lecture spécifique indisponible : la lecture Windows brute est utilisée. Certaines commandes peuvent être absentes ou mal placées.");

    // Ouvre le lecteur et attend sa première lecture ; en cas d'échec d'un lecteur spécifique,
    // bascule sur la lecture Windows brute (warning renseigné). Bloquant : appeler hors du thread UI.
    public static (IControllerInputReader Reader, ControllerDetectionResult First, string? Warning) Open(
        RawGameController controller, CancellationToken token)
    {
        IControllerInputReader reader = Create(controller, out string? warning);
        try
        {
            return (reader, reader.WaitForReading(token), warning);
        }
        catch (OperationCanceledException)
        {
            reader.Dispose();
            throw;
        }
        catch (Exception) when (reader is not WindowsRawControllerReader)
        {
            reader.Dispose();
            reader = CreateRaw(controller);
            return (reader, reader.WaitForReading(token), FallbackMessage);
        }
    }
}

// ============================================================================
// FIN FONCTIONNALITÉ : CHOIX CENTRAL DU LECTEUR D'ENTRÉES
// ============================================================================
