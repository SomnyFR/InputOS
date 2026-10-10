namespace InputOS.Models;

// ============================================================================
// FONCTIONNALITÉ : INFORMATIONS DÉTECTÉES AUTOMATIQUEMENT
// ============================================================================

public sealed record ControllerDetectionResult(
    string DisplayName,
    ushort VendorId,
    ushort ProductId,
    bool IsWireless,
    bool HasStandardGamepad,
    string[] ButtonLabels,
    bool[] ButtonStates,
    double[] AxisValues,
    string[] SwitchPositions)
{
    public bool? MicrophonePressed { get; init; }
}

// ============================================================================
// FIN FONCTIONNALITÉ : INFORMATIONS DÉTECTÉES AUTOMATIQUEMENT
// ============================================================================
