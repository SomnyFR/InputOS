using static InputOS.Services.LocalizationService;
using HidSharp;
using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public sealed class NintendoProControllerReader : IDisposable
{
    // ============================================================================
    // FONCTIONNALITÉ : LECTURE NATIVE SWITCH PRO USB (HAC-013)
    // ============================================================================
    public const string ReadingFormat = "NintendoProUsbV1";
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;
    private ControllerDetectionResult? latest;
    private long lastReport;
    public ControllerDetectionResult? Reading => Environment.TickCount64 - Interlocked.Read(ref lastReport) < 250
        ? Volatile.Read(ref latest) : null;

    public static bool IsSupported(RawGameController controller) => !controller.IsWireless &&
        controller.HardwareVendorId == 0x057E && controller.HardwareProductId == 0x2009;

    public NintendoProControllerReader(RawGameController controller)
    {
        if (!IsSupported(controller) || controller.ButtonCount != 18 ||
            controller.AxisCount != 4 || controller.SwitchCount != 1)
            throw new InvalidOperationException("Format Switch Pro USB non reconnu.");
        string identity = controller.NonRoamableId;
        worker = Task.Run(() => Read(identity));
    }

    public ControllerDetectionResult WaitForReading(CancellationToken token)
    {
        long deadline = Environment.TickCount64 + 4000;
        while (Environment.TickCount64 < deadline)
        {
            token.ThrowIfCancellationRequested();
            cancellation.Token.ThrowIfCancellationRequested();
            if (Reading is { } reading) return reading;
            if (worker.IsCompleted) break;
            token.WaitHandle.WaitOne(10);
        }
        throw new InvalidOperationException(Tr("Lecture Switch Pro indisponible. Rebranchez la manette et fermez les autres logiciels de manette."));
    }

    private void Read(string identity)
    {
        try
        {
            var devices = DeviceList.Local.GetHidDevices(0x057E, 0x2009)
                .Where(device => device.GetMaxInputReportLength() == 64 && device.GetMaxOutputReportLength() == 64).ToArray();
            var matched = devices.Where(device => device.DevicePath.Equals(identity, StringComparison.OrdinalIgnoreCase)).ToArray();
            // Une seule correspondance : ne jamais initialiser une autre manette au hasard.
            var device = matched.Length == 1 ? matched[0] : devices.Length == 1 ? devices[0] : null;
            if (device == null || cancellation.IsCancellationRequested || !device.TryOpen(out HidStream stream)) return;
            using (stream)
            {
                stream.ReadTimeout = 100;
                stream.WriteTimeout = 100;
                var report = new byte[64];
                // Protocole USB Nintendo : handshake, débit, second handshake, maintien USB.
                // https://github.com/torvalds/linux/blob/master/drivers/hid/hid-nintendo.c
                try
                {
                    UsbCommand(stream, report, 0x02);
                    UsbCommand(stream, report, 0x03);
                    UsbCommand(stream, report, 0x02);
                    var usb = new byte[64]; usb[0] = 0x80; usb[1] = 0x04;
                    stream.Write(usb);
                    stream.Write(CreateReportModeCommand());
                    long deadline = Environment.TickCount64 + 1000;
                    bool acknowledged = false;
                    while (!acknowledged && Environment.TickCount64 < deadline && !cancellation.IsCancellationRequested)
                    {
                        try
                        {
                            int length = stream.Read(report, 0, report.Length);
                            acknowledged = length >= 15 && report[0] == 0x21 && report[14] == 0x03 && (report[13] & 0x80) != 0;
                        }
                        catch (TimeoutException) { }
                    }
                    if (!acknowledged) return;
                    while (!cancellation.IsCancellationRequested)
                    {
                        try
                        {
                            int length = stream.Read(report, 0, report.Length);
                            if (ParseReading(report, length) is not { } reading) continue;
                            Volatile.Write(ref latest, reading);
                            Interlocked.Exchange(ref lastReport, Environment.TickCount64);
                        }
                        catch (TimeoutException) { }
                    }
                }
                finally
                {
                    // Rétablir le timeout USB à la fermeture, sans modifier la mémoire de la manette.
                    try { var reset = new byte[64]; reset[0] = 0x80; reset[1] = 0x05; stream.Write(reset); }
                    catch (Exception) { }
                }
            }
        }
        catch (Exception)
        {
            // L'absence de lecture HID est signalée à l'interface ; aucun retour aux entrées Windows erronées.
        }
        finally { Interlocked.Exchange(ref lastReport, 0); }
    }

    private void UsbCommand(HidStream stream, byte[] input, byte command)
    {
        cancellation.Token.ThrowIfCancellationRequested();
        var output = new byte[64]; output[0] = 0x80; output[1] = command;
        stream.Write(output);
        long deadline = Environment.TickCount64 + 600;
        while (Environment.TickCount64 < deadline && !cancellation.IsCancellationRequested)
        {
            try
            {
                int length = stream.Read(input, 0, input.Length);
                if (length >= 2 && input[0] == 0x81 && input[1] == command) return;
            }
            catch (TimeoutException) { }
        }
        throw new InvalidOperationException(Tr("Initialisation USB Nintendo non confirmée."));
    }

    public static byte[] CreateReportModeCommand()
    {
        var output = new byte[64];
        output[0] = 0x01;
        // Vibrations neutres, sous-commande 03 : rapports complets 30.
        byte[] neutral = [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40];
        neutral.CopyTo(output, 2);
        output[10] = 0x03; output[11] = 0x30;
        return output;
    }

    public static ControllerDetectionResult? ParseReading(byte[] report, int length)
    {
        // Accepter uniquement les rapports d'entrées complets, jamais les ACK/horodatages/IMU comme commandes.
        // https://github.com/dekuNukem/Nintendo_Switch_Reverse_Engineering/blob/master/bluetooth_hid_notes.md
        if (length < 49 || length > report.Length || report[0] != 0x30) return null;
        var buttons = new bool[18];
        int[] bits = [0, 1, 2, 3, 6, 7, 8, 9, 10, 11, 12, 13, 16, 17, 18, 19, 22, 23];
        for (int index = 0; index < bits.Length; index++)
            if (index is < 12 or > 15)
                buttons[index] = (report[3 + bits[index] / 8] & (1 << (bits[index] % 8))) != 0;
        // La croix n'est exposée qu'une fois, via le switch, pour éviter les doubles détections.
        int x = ((report[5] & 4) != 0 ? 1 : 0) - ((report[5] & 8) != 0 ? 1 : 0);
        int y = ((report[5] & 1) != 0 ? 1 : 0) - ((report[5] & 2) != 0 ? 1 : 0);
        string hat = (x, y) switch {
            (0, -1) => "Up", (1, -1) => "UpRight", (1, 0) => "Right", (1, 1) => "DownRight",
            (0, 1) => "Down", (-1, 1) => "DownLeft", (-1, 0) => "Left", (-1, -1) => "UpLeft", _ => "Center"
        };
        double[] axes = new double[4];
        for (int stick = 0; stick < 2; stick++)
        {
            int start = 6 + stick * 3;
            axes[stick * 2] = (report[start] | ((report[start + 1] & 15) << 8)) / 4095d;
            axes[stick * 2 + 1] = ((report[start + 1] >> 4) | (report[start + 2] << 4)) / 4095d;
        }
        return new("Nintendo Switch Pro", 0x057E, 0x2009, false, false,
            ["Y", "X", "B", "A", "R", "ZR", "−", "+", "R3", "L3", "Home", "Capture",
             "Croix : switch", "Croix : switch", "Croix : switch", "Croix : switch", "L", "ZL"], buttons, axes, [hat])
            { InputReadingFormat = ReadingFormat };
    }

    public void Dispose() => cancellation.Cancel();
    public async Task StopAsync()
    {
        cancellation.Cancel();
        await worker;
    }
    // ============================================================================
    // FIN FONCTIONNALITÉ : LECTURE NATIVE SWITCH PRO USB
    // ============================================================================
}
