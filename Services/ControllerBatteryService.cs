using System;
using System.Linq;
using System.Threading.Tasks;
using HidSharp;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class ControllerBatteryService
{

    // ============================================================================
    // FONCTIONNALITÉ : DÉTECTION DE LA BATTERIE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const int SonyVendorId =
        0x054C;


    private const int DualShock4V1ProductId =
        0x05C4;


    private const int DualShock4V2ProductId =
        0x09CC;


    private const int ReadTimeoutMilliseconds =
        750;


    private const int MaximumReadAttempts =
        6;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public static Task<string?> GetBatteryDisplayAsync(
        RawGameController controller)
    {
        if (!IsDualShock4(
                controller))
        {
            return Task.FromResult<string?>(
                null);
        }


        return Task.Run(
            () =>
                ReadDualShock4Battery(
                    controller.HardwareVendorId,
                    controller.HardwareProductId));
    }


    private static bool IsDualShock4(
        RawGameController controller)
    {
        if (controller.HardwareVendorId !=
            SonyVendorId)
        {
            return false;
        }


        return controller.HardwareProductId ==
                   DualShock4V1ProductId ||
               controller.HardwareProductId ==
                   DualShock4V2ProductId;
    }


    private static string? ReadDualShock4Battery(
        ushort vendorId,
        ushort productId)
    {
        try
        {
            HidDevice[] devices =
                DeviceList.Local
                    .GetHidDevices(
                        vendorId,
                        productId)
                    .OrderByDescending(
                        device =>
                            device.GetMaxInputReportLength())
                    .ToArray();


            foreach (
                HidDevice device
                in devices)
            {
                if (device.GetMaxInputReportLength() <
                    64)
                {
                    continue;
                }


                if (!device.TryOpen(
                        out HidStream? stream))
                {
                    continue;
                }


                using (stream)
                {
                    stream.ReadTimeout =
                        ReadTimeoutMilliseconds;


                    byte[] report =
                        new byte[
                            device.GetMaxInputReportLength()];


                    for (
                        int attempt = 0;
                        attempt < MaximumReadAttempts;
                        attempt++)
                    {
                        try
                        {
                            int length =
                                stream.Read(
                                    report,
                                    0,
                                    report.Length);


                            if (TryParseDualShock4Battery(
                                    report,
                                    length,
                                    out string? display))
                            {
                                return display;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // Un autre essai peut recevoir
                            // le prochain rapport HID.
                        }
                    }
                }
            }
        }
        catch
        {
            // Une erreur de lecture batterie ne doit jamais
            // empêcher InputOS de fonctionner.
        }


        return null;
    }


    private static bool TryParseDualShock4Battery(
        byte[] report,
        int length,
        out string? display)
    {
        display =
            null;


        if (length <= 0)
        {
            return false;
        }


        int statusIndex;


        if (report[0] == 0x01 &&
            length >= 31)
        {
            // Rapport USB complet du DualShock 4.
            statusIndex =
                30;
        }
        else if (report[0] == 0x11 &&
                 length >= 33)
        {
            // Rapport Bluetooth complet du DualShock 4.
            statusIndex =
                32;
        }
        else
        {
            return false;
        }


        byte status =
            report[statusIndex];


        int batteryValue =
            status & 0x0F;


        bool cableConnected =
            (status & 0x10) != 0;


        if (!cableConnected)
        {
            int percentage =
                batteryValue < 10
                    ? (batteryValue * 10) + 5
                    : 100;


            display =
                $"≈ {percentage} % — Décharge";


            return true;
        }


        if (batteryValue < 10)
        {
            int percentage =
                (batteryValue * 10) + 5;


            display =
                $"≈ {percentage} % — En charge";


            return true;
        }


        if (batteryValue == 10)
        {
            display =
                "100 % — En charge";


            return true;
        }


        if (batteryValue == 11)
        {
            display =
                "100 % — Chargée";


            return true;
        }


        if (batteryValue == 14)
        {
            display =
                "Erreur de charge — Température ou tension";


            return true;
        }


        if (batteryValue == 15)
        {
            display =
                "Erreur de charge";


            return true;
        }


        display =
            "État de charge inconnu";


        return true;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : DÉTECTION DE LA BATTERIE
    // ============================================================================

}