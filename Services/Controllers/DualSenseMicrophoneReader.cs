using HidSharp;
using Windows.Gaming.Input;

namespace InputOS.Services;

public sealed class DualSenseMicrophoneReader : IDisposable
{
    // ============================================================================
    // FONCTIONNALITÉ : LECTURE DU BOUTON MICROPHONE USB PENDANT LA CONFIGURATION
    // ============================================================================

    private readonly CancellationTokenSource cancellation = new();
    private int state;
    private long lastReport;

    public bool? Pressed => Environment.TickCount64 - Interlocked.Read(ref lastReport) < 250
        ? Volatile.Read(ref state) != 0 : null;

    public DualSenseMicrophoneReader(RawGameController controller)
    {
        if (controller.IsWireless || controller.HardwareVendorId != 0x054C ||
            controller.HardwareProductId is not (0x0CE6 or 0x0DF2)) return;
        string identity = controller.NonRoamableId;
        ushort product = controller.HardwareProductId;
        _ = Task.Run(() => Read(identity, product));
    }

    private void Read(string identity, ushort product)
    {
        try
        {
            var devices = DeviceList.Local.GetHidDevices(0x054C, product)
                .Where(device => device.GetMaxInputReportLength() == 64).ToArray();
            var matched = devices.Where(device =>
                device.DevicePath.Equals(identity, StringComparison.OrdinalIgnoreCase)).ToArray();
            // Ne jamais lire arbitrairement une autre manette du même modèle.
            var device = matched.Length == 1 ? matched[0] : devices.Length == 1 ? devices[0] : null;
            if (device == null || cancellation.IsCancellationRequested || !device.TryOpen(out HidStream stream)) return;
            using (stream)
            {
                stream.ReadTimeout = 100;
                var report = new byte[64];
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        int length = stream.Read(report, 0, report.Length);
                        if (!TryParse(report, length, out bool pressed)) continue;
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
        if (length != 64 || report.Length < length || report[0] != 0x01) return false;
        pressed = (report[10] & 0x04) != 0;
        return true;
    }

    public void Dispose() => cancellation.Cancel();

    // ============================================================================
    // FIN FONCTIONNALITÉ : LECTURE DU BOUTON MICROPHONE USB PENDANT LA CONFIGURATION
    // ============================================================================
}
