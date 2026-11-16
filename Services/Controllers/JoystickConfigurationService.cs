using static InputOS.Services.LocalizationService;
using InputOS.Models;

namespace InputOS.Services;

public sealed class JoystickConfigurationService
{

    // ============================================================================
    // FONCTIONNALITÉ : TEST GUIDÉ DES JOYSTICKS
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private double[]? center;
    private int positiveAxis;
    private double positiveEnd;
    private readonly List<JoystickAxisConfiguration> axes = new();

    // Un stick = une paire X/Y. Moins de 4 axes : un seul stick (2 à 3 axes) ou aucun (0 à 1 axe).
    public int StickCount { get; }

    public int TotalSteps => StickCount * 5;

    public int Step { get; private set; }

    public bool IsComplete => Step == TotalSteps;

    public JoystickConfigurationService(int axisCount = 4) =>
        StickCount = axisCount >= 4 ? 2 : axisCount >= 2 ? 1 : 0;

    public JoystickConfiguration? Result { get; private set; }

    public string Instruction => IsComplete
        ? Tr(StickCount == 1 ? "Le joystick est identifié. Bougez-le pour vérifier les valeurs."
            : "Les deux joysticks sont identifiés. Bougez-les pour vérifier les valeurs.")
        : (Step % 5) switch
        {
            0 when StickCount == 1 => Tr("Relâchez le joystick, puis validez sa position au repos."),
            0 => Tf("Relâchez les deux joysticks, puis validez leur position au repos ({0}).", Tr(StickName)),
            1 => Tf("Maintenez le joystick {0} complètement à DROITE (X), puis validez.", Tr(StickName)),
            2 => Tf("Maintenez le joystick {0} complètement à GAUCHE (X), puis validez.", Tr(StickName)),
            3 => Tf("Maintenez le joystick {0} complètement en HAUT (Y), puis validez.", Tr(StickName)),
            _ => Tf("Maintenez le joystick {0} complètement en BAS (Y), puis validez.", Tr(StickName))
        };

    private string StickName => Step < 5 ? Tr("gauche") : Tr("droit");


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public void Capture(IReadOnlyList<double[]> samples)
    {
        if (IsComplete)
        {
            return;
        }

        if (samples.Count < 5)
        {
            throw new InvalidOperationException(Tr("Maintenez la position un court instant avant de valider."));
        }

        int count = samples[0].Length;
        if (count < StickCount * 2 || samples.Any(sample => sample.Length != count ||
            sample.Any(value => !double.IsFinite(value))))
        {
            throw new InvalidOperationException(Tr("Les données des axes ne permettent pas ce test."));
        }

        double[] current = Enumerable.Range(0, count)
            .Select(index => samples.Average(sample => sample[index])).ToArray();

        for (int i = 0; i < count; i++)
        {
            if (samples.Max(sample => sample[i]) - samples.Min(sample => sample[i]) > 0.06)
            {
                throw new InvalidOperationException(Tr("La position bouge encore. Maintenez le joystick immobile, puis réessayez."));
            }
        }

        if (Step % 5 == 0)
        {
            center = current;
            Step++;
            return;
        }

        if (center == null || center.Length != count)
        {
            throw new InvalidOperationException(Tr("Recommencez le test : le nombre d’axes a changé."));
        }

        int[] candidates = Enumerable.Range(0, count)
            .OrderByDescending(index => Math.Abs(current[index] - center[index])).ToArray();
        int candidate = candidates[0];
        double movement = current[candidate] - center[candidate];
        double secondMovement = Math.Abs(current[candidates[1]] - center[candidates[1]]);

        if (Math.Abs(movement) < 0.25)
        {
            throw new InvalidOperationException(Tr("Mouvement trop faible. Poussez le joystick complètement dans la direction demandée."));
        }

        if (secondMovement > Math.Abs(movement) * 0.55)
        {
            throw new InvalidOperationException(Tr("Plusieurs axes bougent. Évitez les diagonales et ne touchez pas aux autres commandes."));
        }

        if (axes.Any(axis => axis.AxisIndex == candidate))
        {
            throw new InvalidOperationException(Tr("Cet axe appartient déjà à une autre direction. Utilisez le joystick et la direction demandés."));
        }

        if (Step % 5 is 1 or 3)
        {
            positiveAxis = candidate;
            positiveEnd = current[candidate];
        }
        else
        {
            if (candidate != positiveAxis ||
                movement * (positiveEnd - center[candidate]) >= 0)
            {
                throw new InvalidOperationException(Tr("La direction opposée ne correspond pas au même axe. Suivez la direction indiquée."));
            }

            axes.Add(new JoystickAxisConfiguration(
                candidate, center[candidate], positiveEnd, current[candidate]));
        }

        Step++;

        if (IsComplete)
        {
            Result = Build(center!, axes);
        }
    }

    // Les axes hors sticks (gâchettes, gaz, curseurs…) sont repris au repos avec la plage nominale 0..1 de Windows.
    // shortcut: type « Slider » par défaut, sans calibration de la course ; le choix du type viendra avec la détection libre.
    public static JoystickConfiguration Build(double[] rest, IReadOnlyList<JoystickAxisConfiguration> sticks)
    {
        var used = sticks.Select(axis => axis.AxisIndex).ToHashSet();
        var extra = Enumerable.Range(0, rest.Length).Where(index => !used.Contains(index)).Select(index =>
        {
            double center = Math.Clamp(rest[index], 0, 1);
            bool up = center <= .5;
            return new JoystickAxisConfiguration(index, center, up ? 1 : 0, up ? 0 : 1) { Type = "Slider" };
        }).ToArray();
        return new JoystickConfiguration(
            sticks.ElementAtOrDefault(0), sticks.ElementAtOrDefault(1),
            sticks.ElementAtOrDefault(2), sticks.ElementAtOrDefault(3)) { Axes = extra };
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : TEST GUIDÉ DES JOYSTICKS
    // ============================================================================

}
