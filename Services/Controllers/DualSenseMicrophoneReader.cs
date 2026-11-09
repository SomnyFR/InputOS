using HidSharp;
using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public sealed class DualSenseMicrophoneReader : IDisposable
{
    // ============================================================================
    // FONCTIONNALITÉ : LECTURE DUALSENSE USB ET BLUETOOTH
    // ============================================================================

    private readonly CancellationTokenSource cancellation = new();
    private int state;
    private long lastReport;
    private ControllerDetectionResult? latestReading;
    public ControllerDetectionResult? Reading => Environment.TickCount64 - Interlocked.Read(ref lastReport) < 250
        ? Volatile.Read(ref latestReading) : null;

    public bool? Pressed => Environment.TickCount64 - Interlocked.Read(ref lastReport) < 250
        ? Volatile.Read(ref state) != 0 : null;

    public DualSenseMicrophoneReader(RawGameController controller)
    {
        if (controller.HardwareVendorId != 0x054C ||
            controller.HardwareProductId is not (0x0CE6 or 0x0DF2)) return;
        string identity = controller.NonRoamableId;
        ushort product = controller.HardwareProductId;
        _ = Task.Run(() => Read(identity, product, controller.IsWireless));
    }

    private void Read(string identity, ushort product, bool wireless)
    {
        try
        {
            var devices = DeviceList.Local.GetHidDevices(0x054C, product)
                .Where(device => device.GetMaxInputReportLength() == (wireless ? 78 : 64)).ToArray();
            var matched = devices.Where(device =>
                device.DevicePath.Equals(identity, StringComparison.OrdinalIgnoreCase)).ToArray();
            // Ne jamais lire arbitrairement une autre manette du même modèle.
            var device = matched.Length == 1 ? matched[0] : devices.Length == 1 ? devices[0] : null;
            if (device == null || cancellation.IsCancellationRequested || !device.TryOpen(out HidStream stream)) return;
            using (stream)
            {
                stream.ReadTimeout = 100;
                if (wireless) {
                    stream.WriteTimeout = 100;
                    stream.Write(CreateBluetoothEnableReport());
                }
                var report = new byte[wireless ? 78 : 64];
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        int length = stream.Read(report, 0, report.Length);
                        if (!TryParse(report, length, out bool pressed)) continue;
                        Volatile.Write(ref latestReading, ParseReading(report, length));
                        Volatile.Write(ref state, pressed ? 1 : 0);
                        Interlocked.Exchange(ref lastReport, Environment.TickCount64);
                    }
                    catch (TimeoutException) { }
                }
            }
        }
        catch (Exception)
        {
            // Un accès HID indisponible ne doit pas interrompre le configurateur.
        }
        finally { Interlocked.Exchange(ref lastReport, 0); }
    }

    public static bool TryParse(byte[] report, int length, out bool pressed)
    {
        pressed = false;
        // Format USB documenté dans le pilote Sony hid-playstation :
        // https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c
        // ReportID + six axes + séquence + buttons[0..3] ; Mic = buttons[2], bit 2.
        if (length < 1 || report.Length < length) return false;
        int offset;
        if (length == 64 && report[0] == 0x01) offset = 1;
        else if (length == 78 && report[0] == 0x31 && HasValidBluetoothChecksum(report)) offset = 2;
        else return false;
        pressed = (report[offset + 9] & 0x04) != 0;
        return true;
    }

    public static ControllerDetectionResult? ParseReading(byte[] report, int length)
    {
        if (!TryParse(report, length, out bool mic)) return null;
        int start = report[0] == 0x31 ? 2 : 1;
        var buttons = new bool[15];
        for (int i = 0; i < 4; i++) buttons[i] = (report[start + 7] & (1 << (4 + i))) != 0;
        for (int i = 0; i < 8; i++) buttons[4 + i] = (report[start + 8] & (1 << i)) != 0;
        buttons[12] = (report[start + 9] & 1) != 0;
        buttons[13] = (report[start + 9] & 2) != 0;
        buttons[14] = mic;
        string[] hats = ["Up", "UpRight", "Right", "DownRight", "Down", "DownLeft", "Left", "UpLeft"];
        int hat = report[start + 7] & 15;
        return new("DualSense", 0x054C, 0x0CE6, start == 2, false, [], buttons,
            [report[start] / 255d, report[start + 1] / 255d, report[start + 2] / 255d,
             report[start + 4] / 255d, report[start + 5] / 255d, report[start + 3] / 255d],
            [hat < 8 ? hats[hat] : "Center"]) { MicrophonePressed = mic };
    }

    public static byte[] CreateBluetoothEnableReport()
    {
        // Rapport sans flags d'effets : active les rapports complets sans changer les LED/vibrations.
        var report = new byte[78]; report[0] = 0x31; report[2] = 0x10;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(74), Checksum(0xA2, report));
        return report;
    }

    private static bool HasValidBluetoothChecksum(byte[] report) =>
        System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(report.AsSpan(74, 4)) == Checksum(0xA1, report);

    private static uint Checksum(byte seed, byte[] report)
    {
        uint crc = uint.MaxValue;
        void Add(byte value) {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
        }
        Add(seed);
        for (int i = 0; i < 74; i++) Add(report[i]);
        return ~crc;
    }

    public void Dispose() => cancellation.Cancel();

    // ============================================================================
    // FIN FONCTIONNALITÉ : LECTURE DUALSENSE USB ET BLUETOOTH
    // ============================================================================
}
