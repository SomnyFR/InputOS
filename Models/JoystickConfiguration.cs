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
    public double Normalize(double rawValue)
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

public sealed record JoystickConfiguration(
    JoystickAxisConfiguration LeftX,
    JoystickAxisConfiguration LeftY,
    JoystickAxisConfiguration RightX,
    JoystickAxisConfiguration RightY);

// ============================================================================
// FIN FONCTIONNALITÉ : AXES IDENTIFIÉS PAR LE TEST DES JOYSTICKS
// ============================================================================
