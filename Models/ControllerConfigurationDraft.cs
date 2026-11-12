namespace InputOS.Models;

// ============================================================================
// FONCTIONNALITÉ : CONFIGURATION TEMPORAIRE D'UNE MANETTE
// ============================================================================

// Conservé uniquement en mémoire pendant le parcours de configuration.
public sealed record ControllerConfigurationDraft(
    string Name,
    ushort VendorId,
    ushort ProductId)
{
    public string Connection { get; init; } = "USB";

    public string ControllerType { get; init; } = "Gamepad";

    public ControllerDetectionResult? Detection { get; init; }

    public JoystickConfiguration? Joysticks { get; init; }

    public ControllerButtonConfiguration? Buttons { get; init; }
}

// ============================================================================
// FIN FONCTIONNALITÉ : CONFIGURATION TEMPORAIRE D'UNE MANETTE
// ============================================================================
