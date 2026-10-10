namespace InputOS.Models;

public sealed record ControllerLocalProfile(
    int SchemaVersion, string ProfileType, bool ValidatedByInputOS, string Model,
    ushort VendorId, ushort ProductId, string Connection,
    int ButtonCount, int AxisCount, int SwitchCount,
    JoystickConfiguration Joysticks, ControllerButtonConfiguration Buttons);
