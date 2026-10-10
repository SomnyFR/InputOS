using InputOS.Models;

namespace InputOS.Services;

public sealed class ControllerButtonConfigurationService
{
    // ============================================================================
    // FONCTIONNALITÉ : IDENTIFICATION GUIDÉE DES BOUTONS ET GÂCHETTES
    // ============================================================================

    public ControllerButtonDefinition[] Definitions { get; }
    public List<ControllerButtonMapping> Mappings { get; } = new();
    public List<string> UnavailableButtons { get; } = new();
    public int Step { get; private set; }
    public bool IsComplete => Step == Definitions.Length;
    public ControllerButtonDefinition? Current => IsComplete ? null : Definitions[Step];
    private readonly double[] restAxes;
    private readonly HashSet<int> joystickAxes;
    private readonly HashSet<int> associatedButtons = new();
    public bool WaitingForRelease { get; private set; }

    public ControllerButtonConfigurationService(string model, double[] rest,
        JoystickConfiguration joysticks)
    {
        Definitions = ControllerButtonCatalogService.GetButtons(model);
        restAxes = (double[])rest.Clone();
        joystickAxes = new(new[] { joysticks.LeftX.AxisIndex, joysticks.LeftY.AxisIndex,
            joysticks.RightX.AxisIndex, joysticks.RightY.AxisIndex });
    }

    public bool IsAtRest(ControllerDetectionResult reading) =>
        reading.MicrophonePressed != true && !reading.ButtonStates.Any(value => value) &&
        reading.SwitchPositions.All(position => position == "Center") &&
        reading.AxisValues.Length == restAxes.Length &&
        Enumerable.Range(0, restAxes.Length).Where(index => !joystickAxes.Contains(index))
            .All(index => Math.Abs(reading.AxisValues[index] - restAxes[index]) < 0.12);

    public void ObserveRelease(ControllerDetectionResult reading)
    {
        if (WaitingForRelease && IsAtRest(reading)) WaitingForRelease = false;
    }

    public void Capture(IReadOnlyList<ControllerDetectionResult> samples)
    {
        if (Current == null) return;
        if (WaitingForRelease) throw new InvalidOperationException("Relâchez la commande précédente avant de continuer.");
        if (samples.Count < 8) throw new InvalidOperationException("Maintenez la commande un court instant pour permettre son enregistrement automatique.");
        var candidates = samples.Select(FindInput).ToArray();
        if (candidates.Any(input => input.InputType != candidates[0].InputType ||
            input.InputIndex != candidates[0].InputIndex || input.SwitchPosition != candidates[0].SwitchPosition))
            throw new InvalidOperationException("L’appui n’est pas stable. Maintenez uniquement la commande demandée.");
        var mapping = candidates[^1];
        if (mapping.InputType == "Axis" &&
            samples.Max(sample => sample.AxisValues[mapping.InputIndex]) -
            samples.Min(sample => sample.AxisValues[mapping.InputIndex]) > 0.08)
            throw new InvalidOperationException("La gâchette bouge encore. Maintenez-la complètement enfoncée pour permettre son enregistrement automatique.");
        if (Mappings.Any(previous => previous.InputType == mapping.InputType &&
            previous.InputIndex == mapping.InputIndex && previous.SwitchPosition == mapping.SwitchPosition &&
            (mapping.InputType != "Axis" || Math.Sign(previous.PressedValue!.Value - previous.RestValue!.Value) ==
                Math.Sign(mapping.PressedValue!.Value - mapping.RestValue!.Value))))
            throw new InvalidOperationException("Cette entrée est déjà associée à une autre commande. Appuyez sur la commande demandée.");
        Mappings.Add(mapping);
        foreach (var sample in samples)
            for (int index = 0; index < sample.ButtonStates.Length; index++)
                if (sample.ButtonStates[index]) associatedButtons.Add(index);
        Step++;
        WaitingForRelease = true;
    }

    private ControllerButtonMapping FindInput(ControllerDetectionResult reading)
    {
        var definition = Current!;
        if (reading.AxisValues.Length != restAxes.Length)
            throw new InvalidOperationException("Le nombre d’axes a changé. Recommencez le test.");
        int[] analog = definition.IsTrigger ? Enumerable.Range(0, restAxes.Length)
            .Where(index => !joystickAxes.Contains(index) && Math.Abs(reading.AxisValues[index] - restAxes[index]) > 0.35)
            .ToArray() : Array.Empty<int>();
        int[] buttons = Enumerable.Range(0, reading.ButtonStates.Length).Where(index => reading.ButtonStates[index]).ToArray();
        int[] switches = Enumerable.Range(0, reading.SwitchPositions.Length)
            .Where(index => reading.SwitchPositions[index] != "Center").ToArray();
        if (reading.MicrophonePressed == true)
        {
            // Le même appui peut être exposé à la fois en HID et par Windows.
            // Accepter cette entrée supplémentaire uniquement si elle n'est pas
            // déjà associée à une autre commande pendant ce test.
            bool microphoneOnly = buttons.Length <= 1 && switches.Length == 0 &&
                !buttons.Any(associatedButtons.Contains);
            if (definition.Id == "Microphone" && microphoneOnly)
                return new(definition.Id, definition.Name, "HidButton", 10, "Bit2");
            if (definition.Id == "Microphone")
                throw new InvalidOperationException("Le bouton microphone est détecté, mais une autre commande est également active. Relâchez les autres commandes, puis maintenez uniquement Mic.");
            throw new InvalidOperationException("Relâchez le bouton microphone et maintenez uniquement la commande demandée.");
        }
        if (analog.Length == 1 && buttons.Length <= 1 && switches.Length == 0)
            return new(definition.Id, definition.Name, "Axis", analog[0],
                RestValue: restAxes[analog[0]], PressedValue: reading.AxisValues[analog[0]]);
        if (buttons.Length == 1 && switches.Length == 0 && analog.Length == 0)
        {
            if (associatedButtons.Contains(buttons[0]))
                throw new InvalidOperationException("Cette commande est déjà associée. Relâchez-la et appuyez sur la commande demandée.");
            return new(definition.Id, definition.Name, "Button", buttons[0]);
        }
        if (definition.IsDirection && buttons.Length == 0 && switches.Length == 1 && analog.Length == 0 &&
            reading.SwitchPositions[switches[0]] is "Up" or "Down" or "Left" or "Right")
            return new(definition.Id, definition.Name, "Switch", switches[0], reading.SwitchPositions[switches[0]]);
        if (definition.Id == "Microphone" && reading.MicrophonePressed == null && buttons.Length == 0)
            throw new InvalidOperationException("La lecture du bouton microphone est indisponible. Vérifiez la connexion USB et fermez les autres logiciels de manette, puis recommencez le test. Vous pouvez aussi signaler cette commande comme non détectée.");
        throw new InvalidOperationException("Appui non identifié ou plusieurs commandes actives. Relâchez tout, puis maintenez uniquement la commande demandée.");
    }

    public void Skip()
    {
        if (Current == null) return;
        UnavailableButtons.Add(Current.Name);
        Step++;
        WaitingForRelease = true;
    }

    public ControllerButtonConfiguration GetResult()
    {
        if (!IsComplete) throw new InvalidOperationException("Le test n’est pas terminé.");
        return new(Mappings.ToArray(), UnavailableButtons.ToArray());
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : IDENTIFICATION GUIDÉE DES BOUTONS ET GÂCHETTES
    // ============================================================================
}
