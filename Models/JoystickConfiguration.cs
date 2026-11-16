using System.Text.Json.Serialization;

namespace InputOS.Models;

// ============================================================================
// FONCTIONNALITÉ : AXES IDENTIFIÉS PAR LE TEST DES JOYSTICKS
// ============================================================================

public sealed record JoystickAxisConfiguration(
    int AxisIndex,
    double Center,
    double PositiveEnd,
    double NegativeEnd)
{
    // Profils v1 : tous les axes sont des sticks, non inversés.
    public string Type { get; init; } = "Stick";
    public bool Inverted { get; init; }

    // Profils « Autre » : nom de l'entrée (Roll, Throttle…) et zone morte (0 à 0,5), absents du JSON s'ils ne servent pas.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Deadzone { get; init; }

    public double Normalize(double rawValue)
    {
        double value = NormalizeRaw(rawValue);
        if (Math.Abs(value) < Deadzone) value = 0;
        return Inverted ? -value : value;
    }

    private double NormalizeRaw(double rawValue)
    {
        double delta = rawValue - Center;
        double positiveDelta = PositiveEnd - Center;
        double negativeDelta = NegativeEnd - Center;

        return Math.Clamp(
            delta * positiveDelta >= 0
                ? delta / positiveDelta
                : -delta / negativeDelta,
            -1.0,
            1.0);
    }
}

// Les sticks sont des paires X/Y optionnelles ; Axes contient les autres axes (gâchette, manette des gaz, curseur, molette).
public sealed record JoystickConfiguration(
    JoystickAxisConfiguration? LeftX,
    JoystickAxisConfiguration? LeftY,
    JoystickAxisConfiguration? RightX,
    JoystickAxisConfiguration? RightY)
{
    public JoystickAxisConfiguration[] Axes { get; init; } = [];

    public JoystickAxisConfiguration[] StickAxes() =>
        new[] { LeftX, LeftY, RightX, RightY }.OfType<JoystickAxisConfiguration>().ToArray();

    public JoystickAxisConfiguration[] AllAxes() => [.. StickAxes(), .. Axes];
}

// ============================================================================
// FIN FONCTIONNALITÉ : AXES IDENTIFIÉS PAR LE TEST DES JOYSTICKS
// ============================================================================
