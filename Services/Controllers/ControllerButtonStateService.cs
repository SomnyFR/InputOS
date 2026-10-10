using InputOS.Models;

namespace InputOS.Services;

public static class ControllerButtonStateService
{
    // ============================================================================
    // FONCTIONNALITÉ : AFFICHAGE DES COMMANDES ASSOCIÉES EN DIRECT
    // ============================================================================

    public static bool IsPressed(ControllerButtonMapping mapping, ControllerDetectionResult reading)
    {
        int index = mapping.InputIndex;
        return mapping.InputType switch
        {
            "Button" => index >= 0 && index < reading.ButtonStates.Length && reading.ButtonStates[index],
            "HidButton" => index == 10 && mapping.SwitchPosition == "Bit2" && reading.MicrophonePressed == true,
            "Switch" => index >= 0 && index < reading.SwitchPositions.Length &&
                IsDirectionPressed(mapping.SwitchPosition, reading.SwitchPositions[index]),
            "Axis" => index >= 0 && index < reading.AxisValues.Length &&
                mapping.RestValue is double rest && mapping.PressedValue is double pressed &&
                Math.Abs(pressed - rest) > 0.001 &&
                (reading.AxisValues[index] - rest) / (pressed - rest) >= 0.2,
            _ => false
        };
    }

    private static bool IsDirectionPressed(string? direction, string position) => direction switch
    {
        "Up" => position is "Up" or "UpLeft" or "UpRight",
        "Down" => position is "Down" or "DownLeft" or "DownRight",
        "Left" => position is "Left" or "UpLeft" or "DownLeft",
        "Right" => position is "Right" or "UpRight" or "DownRight",
        _ => false
    };

    // ============================================================================
    // FIN FONCTIONNALITÉ : AFFICHAGE DES COMMANDES ASSOCIÉES EN DIRECT
    // ============================================================================
}
