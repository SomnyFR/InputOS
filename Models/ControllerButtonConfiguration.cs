namespace InputOS.Models;

// ============================================================================
// FONCTIONNALITÉ : COMMANDES DU MODÈLE ET ASSOCIATIONS TEMPORAIRES
// ============================================================================

public sealed record ControllerButtonDefinition(
    string Id, string Name, string Label, double X, double Y,
    bool IsTrigger = false, bool IsDirection = false);

public sealed record ControllerButtonMapping(
    string Id, string Name, string InputType, int InputIndex,
    string? SwitchPosition = null, double? RestValue = null, double? PressedValue = null);

public sealed record ControllerButtonConfiguration(
    ControllerButtonMapping[] Mappings, string[] UnavailableButtons);

// ============================================================================
// FIN FONCTIONNALITÉ : COMMANDES DU MODÈLE ET ASSOCIATIONS TEMPORAIRES
// ============================================================================
