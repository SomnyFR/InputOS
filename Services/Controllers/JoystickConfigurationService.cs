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

    public int Step { get; private set; }

    public bool IsComplete => Step == 10;

    public JoystickConfiguration? Result { get; private set; }

    public string Instruction => IsComplete
        ? Tr("Les deux joysticks sont identifiés. Bougez-les pour vérifier les valeurs.")
        : (Step % 5) switch
        {
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
        if (count < 4 || samples.Any(sample => sample.Length != count ||
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
            Result = new JoystickConfiguration(axes[0], axes[1], axes[2], axes[3]);
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : TEST GUIDÉ DES JOYSTICKS
    // ============================================================================

}
