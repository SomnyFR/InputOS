using InputOS.Models;
using Windows.Gaming.Input;

namespace InputOS.Services;

public static class ControllerAutoDetectionService
{

    // ============================================================================
    // FONCTIONNALITÉ : DÉTECTION AUTOMATIQUE DES INFORMATIONS MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public static bool IsJoystickAxis(
        ushort vendorId,
        ushort productId,
        int axisIndex)
    {
        // Reprendre les dispositions utilisées par le testeur InputOS.
        // Les axes d'un modèle inconnu restent bruts jusqu'à leur identification.
        if (vendorId == 0x054C &&
            productId is 0x05C4 or 0x09CC or 0x0DF2)
        {
            return axisIndex is 0 or 1 or 2 or 5;
        }


        if (vendorId == 0x045E && productId == 0x0B22)
        {
            return axisIndex is 0 or 1 or 2 or 3;
        }


        return false;
    }


    public static ControllerDetectionResult Detect(
        RawGameController controller)
    {
        if (controller.IsWireless)
        {
            throw new InvalidOperationException("Une connexion filaire est requise.");
        }


        if (!RawGameController.RawGameControllers.Contains(controller))
        {
            throw new InvalidOperationException("La manette est déconnectée.");
        }


        bool[] buttons =
            new bool[controller.ButtonCount];


        double[] axes =
            new double[controller.AxisCount];


        GameControllerSwitchPosition[] switches =
            new GameControllerSwitchPosition[controller.SwitchCount];


        // Un instantané suffit : aucune manipulation ni calibration.
        controller.GetCurrentReading(buttons, switches, axes);


        string[] labels =
            new string[buttons.Length];


        for (int i = 0; i < labels.Length; i++)
        {
            try
            {
                GameControllerButtonLabel label =
                    controller.GetButtonLabel(i);


                labels[i] =
                    label == GameControllerButtonLabel.None
                        ? "Non identifié"
                        : label.ToString();
            }
            catch (Exception)
            {
                // Certains pilotes ne fournissent pas les noms des boutons.
                labels[i] =
                    "Non identifié";
            }
        }


        bool hasStandardGamepad;


        try
        {
            hasStandardGamepad =
                Gamepad.FromGameController(controller) != null;
        }
        catch (Exception)
        {
            hasStandardGamepad =
                false;
        }


        if (!RawGameController.RawGameControllers.Contains(controller))
        {
            throw new InvalidOperationException("La manette est déconnectée.");
        }


        return new ControllerDetectionResult(
            controller.DisplayName,
            controller.HardwareVendorId,
            controller.HardwareProductId,
            controller.IsWireless,
            hasStandardGamepad,
            labels,
            buttons,
            axes,
            switches.Select(position => position.ToString()).ToArray());
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : DÉTECTION AUTOMATIQUE DES INFORMATIONS MANETTE
    // ============================================================================

}
