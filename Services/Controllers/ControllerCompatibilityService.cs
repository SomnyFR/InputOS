using Windows.Gaming.Input;

namespace InputOS.Services;

public static class ControllerCompatibilityService
{

    // ============================================================================
    // FONCTIONNALITÉ : COMPATIBILITÉ DES MANETTES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const ushort SonyVendorId =
        0x054C;


    private const ushort MicrosoftVendorId =
        0x045E;


    // Profils des modèles déjà pris en charge par InputOS.
    // La détection d'un fabricant ne valide pas tous ses modèles ou révisions.
    private static readonly Dictionary<(ushort VendorId, ushort ProductId), string>
        validatedProfiles = new()
        {
            [(SonyVendorId, 0x05C4)] = "DualShock 4",
            [(SonyVendorId, 0x09CC)] = "DualShock 4",
            [(SonyVendorId, 0x0DF2)] = "DualSense Edge",
            [(MicrosoftVendorId, 0x0B22)] = "Xbox Elite Series 2"
        };


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public static bool HasValidatedModel(
        string modelName)
    {
        return validatedProfiles.Values.Any(
            profileName => string.Equals(
                profileName,
                modelName,
                StringComparison.OrdinalIgnoreCase));
    }


    public static bool HasValidatedProfile(
        ushort vendorId,
        ushort productId)
    {
        return validatedProfiles.ContainsKey(
            (vendorId, productId));
    }


    public static bool HasValidatedProfile(
        RawGameController controller)
    {
        return HasValidatedProfile(
            controller.HardwareVendorId,
            controller.HardwareProductId);
    }


    public static string GetControllerDisplayName(
        RawGameController controller)
    {
        if (validatedProfiles.TryGetValue(
                (controller.HardwareVendorId, controller.HardwareProductId),
                out string? profileName))
        {
            // Conserver le nom Windows des manettes Xbox lorsqu'il est disponible.
            return controller.HardwareVendorId == MicrosoftVendorId &&
                   !string.IsNullOrWhiteSpace(controller.DisplayName)
                ? controller.DisplayName
                : profileName;
        }


        if (!string.IsNullOrWhiteSpace(controller.DisplayName))
        {
            return controller.DisplayName;
        }


        return controller.HardwareVendorId switch
        {
            SonyVendorId =>
                "Manette PlayStation",

            MicrosoftVendorId =>
                "Manette Xbox",

            _ =>
                "Manette"
        };
    }


    public static string GetControllerType(
        RawGameController controller)
    {
        return controller.HardwareVendorId switch
        {
            SonyVendorId =>
                "PlayStation",

            MicrosoftVendorId =>
                "Xbox",

            _ =>
                "Inconnu"
        };
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : COMPATIBILITÉ DES MANETTES
    // ============================================================================

}
