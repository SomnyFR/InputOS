using static InputOS.Services.LocalizationService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Gaming.Input;
using InputOS.Models;
using InputOS.Services;

namespace InputOS;

public partial class TesterWindow : Window
{

    // ============================================================================
    // FONCTIONNALITÉ : CONFIGURATION DU TESTEUR
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private readonly RawGameController controller;
    private readonly ControllerLocalProfile? inputProfile;
    private readonly IControllerInputReader reader;
    private bool? microphonePressed;
    private readonly long readerStartedAt = Environment.TickCount64;

    private readonly DispatcherTimer inputTimer;

    private readonly bool[] buttonValues;

    private readonly GameControllerSwitchPosition[] switchValues;

    private readonly double[] axisValues;

    private readonly List<Border> buttonIndicators = [];

    private bool invertLeftYAxisVisual;

    private bool invertRightYAxisVisual;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public TesterWindow(
        RawGameController selectedController)
    {
        InitializeComponent();

        controller = selectedController;
        inputProfile = DualSenseProfileService.Load(controller) ?? XboxSeriesProfileService.Load(controller)
            ?? ControllerLocalProfileService.Load(controller);
        // Lecteur choisi par modèle et connexion (profil connu), sinon lecture Windows brute.
        // Un échec du lecteur spécifique retombe sur la lecture brute (avertissement dans le titre).
        string? readerWarning = null;
        reader = inputProfile != null ? ControllerReaderFactory.Create(controller, out readerWarning)
            : ControllerReaderFactory.CreateRaw(controller);


        buttonValues =
            new bool[controller.ButtonCount];


        switchValues =
            new GameControllerSwitchPosition[controller.SwitchCount];


        axisValues =
            new double[controller.AxisCount];


        InitializeControllerInformation();
        if (readerWarning != null) Title += " - " + readerWarning;

        CreateButtonIndicators();

        CreateNamedAxes();


        inputTimer = new DispatcherTimer
        {
            Interval =
                TimeSpan.FromMilliseconds(16)
        };


        inputTimer.Tick +=
            InputTimer_Tick;


        inputTimer.Start();


        Closed +=
            TesterWindow_Closed;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONFIGURATION DU TESTEUR
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : INFORMATIONS MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Aucune configuration spécifique.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void InitializeControllerInformation()
    {
        if (inputProfile != null) Title = inputProfile.ValidatedByInputOS
            ? "InputOS - " + inputProfile.Model + " " + inputProfile.Connection : Tr("InputOS - Profil local provisoire");
        ControllerNameText.Text =
            ControllerCompatibilityService.GetControllerDisplayName(controller);


        ControllerHardwareText.Text =
            Tf("VID : {0} | PID : {1} | Boutons : {2} | Axes : {3} | Switches : {4}",
                controller.HardwareVendorId.ToString("X4"), controller.HardwareProductId.ToString("X4"),
                controller.ButtonCount, controller.AxisCount, controller.SwitchCount);
        if (inputProfile?.Buttons.UnavailableButtons.Length > 0)
            ControllerHardwareText.Text += Tr("\nCommandes non disponibles : ") + string.Join(", ", inputProfile.Buttons.UnavailableButtons.Select(Tr));
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : INFORMATIONS MANETTE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : OPTIONS D'AFFICHAGE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Chaque inversion concerne uniquement la représentation visuelle
    // du stick correspondant.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void LeftInvertYAxisCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        invertLeftYAxisVisual =
            LeftInvertYAxisCheckBox.IsChecked == true;
    }


    private void RightInvertYAxisCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        invertRightYAxisVisual =
            RightInvertYAxisCheckBox.IsChecked == true;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : OPTIONS D'AFFICHAGE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : CRÉATION DES BOUTONS
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const double ButtonWidth = 118;

    private const double ButtonHeight = 92;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void CreateButtonIndicators()
    {
        ButtonsPanel.Children.Clear();

        buttonIndicators.Clear();


        for (int i = 0; i < (inputProfile?.Buttons.Mappings.Length ?? controller.ButtonCount); i++)
        {
            TextBlock buttonText = new()
            {
                Text = inputProfile != null ? Tr(inputProfile.Buttons.Mappings[i].Name) : $"B{i}",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = inputProfile == null ? 14 : 10.5
            };


            UIElement buttonContent = buttonText;

            if (inputProfile != null)
            {
                StackPanel content = new()
                {
                    VerticalAlignment = VerticalAlignment.Center
                };

                FrameworkElement icon =
                    ControllerButtonIconService.Create(
                        inputProfile.Buttons.Mappings[i].Id,
                        inputProfile.Model,
                        buttonText.Text);

                icon.HorizontalAlignment = HorizontalAlignment.Center;
                icon.Margin = new Thickness(0, 0, 0, 5);

                content.Children.Add(icon);
                content.Children.Add(buttonText);

                buttonContent = content;
            }


            Border buttonBorder = new()
            {
                Width = ButtonWidth,
                Height = ButtonHeight,

                Margin =
                    new Thickness(5),

                BorderThickness =
                    new Thickness(1),

                BorderBrush =
                    (System.Windows.Media.Brush)FindResource("AppBorderBrush"),

                Background =
                    (System.Windows.Media.Brush)FindResource("AppCardHoverBrush"),

                CornerRadius =
                    new CornerRadius(10),

                Child =
                    buttonContent
            };


            System.Windows.Documents.TextElement.SetForeground(
                buttonBorder,
                (System.Windows.Media.Brush)FindResource("AppForegroundBrush"));


            buttonIndicators.Add(
                buttonBorder);


            ButtonsPanel.Children.Add(
                buttonBorder);
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CRÉATION DES BOUTONS
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : LECTURE DES ENTRÉES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Lecture environ 60 fois par seconde.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void InputTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!IsActive)
        {
            CenterStickPoints();

            return;
        }


        try
        {
            if (reader.Read() is not { } reading)
            {
                // Lecture périmée : jamais présentée comme actuelle. Le profil Nintendo HID ne vaut pas pour
                // les entrées Windows brutes, donc pas de repli ici : message et arrêt.
                Array.Clear(buttonValues);
                Array.Fill(switchValues, GameControllerSwitchPosition.Center);
                microphonePressed = null;
                UpdateButtons();
                CenterStickPoints();
                if (Environment.TickCount64 - readerStartedAt < 4500) return;
                inputTimer.Stop();
                Title = Tr("InputOS - Lecture Nintendo indisponible : rebranchez la manette en USB");
                return;
            }
            Array.Copy(reading.ButtonStates, buttonValues, Math.Min(reading.ButtonStates.Length, buttonValues.Length));
            Array.Copy(reading.AxisValues, axisValues, Math.Min(reading.AxisValues.Length, axisValues.Length));
            for (int i = 0; i < switchValues.Length && i < reading.SwitchPositions.Length; i++)
                switchValues[i] = Enum.Parse<GameControllerSwitchPosition>(reading.SwitchPositions[i]);
            microphonePressed = reading.MicrophonePressed;

            // En Bluetooth, le rapport HID complet fournit les commandes supplémentaires
            // et un ordre d'axes indépendant du mode de rapport Windows.
            if (inputProfile?.ValidatedByInputOS == true && controller.IsWireless &&
                (reader as DualSenseControllerReader)?.HidReading is { } hid)
            {
                Array.Copy(hid.ButtonStates, buttonValues, Math.Min(hid.ButtonStates.Length, buttonValues.Length));
                Array.Copy(hid.AxisValues, axisValues, axisValues.Length);
                switchValues[0] = Enum.Parse<GameControllerSwitchPosition>(hid.SwitchPositions[0]);
            }

            UpdateButtons();

            UpdateAxes();

            UpdateSticks();

            UpdateDPad();

            UpdateNamedAxes();
        }
        catch
        {
            inputTimer.Stop();

            Title =
                Tr("InputOS - Manette déconnectée");
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : LECTURE DES ENTRÉES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : AFFICHAGE DES BOUTONS
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Blanc = bouton relâché.
    // Gris = bouton appuyé.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    // Profils « Autre » : les axes nommés s'affichent en liste, et le schéma de manette est masqué hors type « Gamepad ».
    private readonly List<(JoystickAxisConfiguration Axis, System.Windows.Controls.ProgressBar Bar, TextBlock Value)> namedAxes = [];

    private void CreateNamedAxes()
    {
        if (inputProfile == null) return;
        if (inputProfile.ControllerType != "Gamepad")
        {
            SticksCard.Visibility = Visibility.Collapsed;
            AxesDPadGrid.Visibility = Visibility.Collapsed;
        }
        foreach (var axis in inputProfile.Joysticks.AllAxes().Where(a => a.Name != null).OrderBy(a => a.AxisIndex))
        {
            var bar = new System.Windows.Controls.ProgressBar { Minimum = -1, Maximum = 1, Height = 10, Margin = new Thickness(0, 0, 12, 0) };
            var value = new TextBlock { Width = 60, TextAlignment = TextAlignment.Right };
            var line = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            line.ColumnDefinitions.Add(new ColumnDefinition());
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var label = new TextBlock { Text = axis.Name };
            Grid.SetColumn(bar, 1);
            Grid.SetColumn(value, 2);
            line.Children.Add(label);
            line.Children.Add(bar);
            line.Children.Add(value);
            NamedAxesPanel.Children.Add(line);
            namedAxes.Add((axis, bar, value));
        }
        if (namedAxes.Count > 0) NamedAxesCard.Visibility = Visibility.Visible;
    }

    private void UpdateNamedAxes()
    {
        foreach (var (axis, bar, value) in namedAxes)
        {
            double v = axis.AxisIndex < axisValues.Length ? axis.Normalize(axisValues[axis.AxisIndex]) : 0;
            bar.Value = v;
            value.Text = v.ToString("0.000");
        }
    }

    private void UpdateButtons()
    {
        var reading = inputProfile == null ? null : new ControllerDetectionResult(inputProfile.Model,
            inputProfile.VendorId, inputProfile.ProductId, false, false, Array.Empty<string>(),
            buttonValues, axisValues, switchValues.Select(value => value.ToString()).ToArray())
            { MicrophonePressed = microphonePressed };
        for (int i = 0; i < buttonIndicators.Count; i++)
        {
            bool pressed = reading == null ? buttonValues[i] : ControllerButtonStateService.IsPressed(inputProfile!.Buttons.Mappings[i], reading);
            Border indicator =
                buttonIndicators[i];


            // Appuyé : fond cyan sombre, pour que les icônes colorées restent lisibles.
            indicator.Background =
                pressed
                    ? new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x0E, 0x4A, 0x5C))
                    : (System.Windows.Media.Brush)FindResource("AppCardHoverBrush");


            indicator.BorderBrush =
                (System.Windows.Media.Brush)FindResource(
                    pressed
                        ? "AppAccentBrush"
                        : "AppBorderBrush");


            indicator.BorderThickness =
                pressed
                    ? new Thickness(2)
                    : new Thickness(1);
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : AFFICHAGE DES BOUTONS
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : MAPPING DES AXES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const ushort MicrosoftVendorId =
        0x045E;


    private const ushort XboxEliteSeries2ProductId =
        0x0B22;


    private readonly record struct ControllerAxisMapping(
        int LeftX,
        int LeftY,
        int RightX,
        int RightY,
        int LeftTrigger,
        int RightTrigger);


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private ControllerAxisMapping GetAxisMapping()
    {
        if (inputProfile != null)
            return new(inputProfile.Joysticks.LeftX?.AxisIndex ?? -1, inputProfile.Joysticks.LeftY?.AxisIndex ?? -1,
                inputProfile.Joysticks.RightX?.AxisIndex ?? -1, inputProfile.Joysticks.RightY?.AxisIndex ?? -1,
                inputProfile.Buttons.Mappings.FirstOrDefault(m => m.Id == "LeftTrigger" && m.InputType == "Axis")?.InputIndex ?? -1,
                inputProfile.Buttons.Mappings.FirstOrDefault(m => m.Id == "RightTrigger" && m.InputType == "Axis")?.InputIndex ?? -1);
        if (controller.HardwareVendorId ==
                MicrosoftVendorId &&
            controller.HardwareProductId ==
                XboxEliteSeries2ProductId)
        {
            return new ControllerAxisMapping(
                LeftX: 1,
                LeftY: 0,
                RightX: 3,
                RightY: 2,
                LeftTrigger: 4,
                RightTrigger: 5);
        }


        // Mapping actuel PlayStation / DualShock 4.

        return new ControllerAxisMapping(
            LeftX: 0,
            LeftY: 1,
            RightX: 2,
            RightY: 5,
            LeftTrigger: 3,
            RightTrigger: 4);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : MAPPING DES AXES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : AFFICHAGE DES AXES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // RawGameController retourne généralement les axes entre 0 et 1.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void UpdateAxes()
    {
        ControllerAxisMapping mapping =
            GetAxisMapping();


        double leftX =
            GetNormalizedStickAxis(
                mapping.LeftX);


        double leftY =
            GetNormalizedStickAxis(
                mapping.LeftY);


        double rightX =
            GetNormalizedStickAxis(
                mapping.RightX);


        double rightY =
            GetNormalizedStickAxis(
                mapping.RightY);


        double leftTrigger =
            GetRawAxis(
                mapping.LeftTrigger);


        double rightTrigger =
            GetRawAxis(
                mapping.RightTrigger);


        LeftXText.Text =
            leftX.ToString("0.000");


        LeftYText.Text =
            leftY.ToString("0.000");


        RightXText.Text =
            rightX.ToString("0.000");


        RightYText.Text =
            rightY.ToString("0.000");


        LeftTriggerText.Text =
            leftTrigger.ToString("0.000");


        RightTriggerText.Text =
            rightTrigger.ToString("0.000");
    }


    private double GetNormalizedStickAxis(
        int axisIndex)
    {
        if (axisIndex < 0 ||
            axisIndex >= axisValues.Length)
        {
            return 0;
        }


        double rawValue =
            axisValues[axisIndex];
        if (inputProfile != null)
        {
            var axis = inputProfile.Joysticks.AllAxes().FirstOrDefault(a => a.AxisIndex == axisIndex);
            if (axis == null) return (rawValue * 2.0) - 1.0;
            double value = axis.Normalize(rawValue);
            return axis == inputProfile.Joysticks.LeftY || axis == inputProfile.Joysticks.RightY ? -value : value;
        }


        return (rawValue * 2.0) - 1.0;
    }


    private double GetRawAxis(
        int axisIndex)
    {
        if (axisIndex < 0 ||
            axisIndex >= axisValues.Length)
        {
            return 0;
        }


        if (inputProfile != null)
        {
            var mapping = inputProfile.Buttons.Mappings.FirstOrDefault(m => m.InputType == "Axis" && m.InputIndex == axisIndex);
            if (mapping?.RestValue is double rest && mapping.PressedValue is double pressed)
                return Math.Clamp((axisValues[axisIndex] - rest) / (pressed - rest), 0, 1);
        }
        return axisValues[axisIndex];
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : AFFICHAGE DES AXES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : VISUALISATION DES STICKS
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const double StickCenter = 99;

    private const double StickRadius = 90;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void UpdateSticks()
    {
        ControllerAxisMapping mapping =
            GetAxisMapping();


        double leftX =
            GetNormalizedStickAxis(
                mapping.LeftX);


        double leftY =
            GetNormalizedStickAxis(
                mapping.LeftY);


        double rightX =
            GetNormalizedStickAxis(
                mapping.RightX);


        double rightY =
            GetNormalizedStickAxis(
                mapping.RightY);


        if (invertLeftYAxisVisual)
        {
            leftY =
                -leftY;
        }


        if (invertRightYAxisVisual)
        {
            rightY =
                -rightY;
        }


        MoveStickPoint(
            LeftStickPoint,
            leftX,
            leftY);


        MoveStickPoint(
            RightStickPoint,
            rightX,
            rightY);
    }


    private void CenterStickPoints()
    {
        CenterStickPoint(
            LeftStickPoint);


        CenterStickPoint(
            RightStickPoint);
    }


    private static void CenterStickPoint(
        FrameworkElement point)
    {
        double pointX =
            StickCenter -
            (point.Width / 2);


        double pointY =
            StickCenter -
            (point.Height / 2);


        Canvas.SetLeft(
            point,
            pointX);


        Canvas.SetTop(
            point,
            pointY);
    }


    private static void MoveStickPoint(
        FrameworkElement point,
        double x,
        double y)
    {
        double magnitude =
            Math.Sqrt(
                (x * x) +
                (y * y));


        if (magnitude > 1.0)
        {
            x /=
                magnitude;


            y /=
                magnitude;
        }


        double pointX =
            StickCenter +
            (x * StickRadius) -
            (point.Width / 2);


        double pointY =
            StickCenter +
            (y * StickRadius) -
            (point.Height / 2);


        Canvas.SetLeft(
            point,
            pointX);


        Canvas.SetTop(
            point,
            pointY);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : VISUALISATION DES STICKS
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : AFFICHAGE DU D-PAD
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Le D-Pad est généralement exposé comme un switch.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void UpdateDPad()
    {
        if (inputProfile != null)
        {
            var reading = new ControllerDetectionResult(inputProfile.Model, inputProfile.VendorId,
                inputProfile.ProductId, false, false, Array.Empty<string>(), buttonValues, axisValues,
                switchValues.Select(value => value.ToString()).ToArray());
            var directions = inputProfile.Buttons.Mappings.Where(m => m.Id.StartsWith("Dpad", StringComparison.Ordinal))
                .Where(m => ControllerButtonStateService.IsPressed(m, reading)).Select(m => Tr(m.Name)).ToArray();
            DPadText.Text = directions.Length == 0 ? Tr("Neutre") : string.Join(" + ", directions);
            return;
        }
        if (switchValues.Length == 0)
        {
            DPadText.Text =
                Tr("Non disponible");

            return;
        }


        DPadText.Text =
            switchValues[0] switch
            {
                GameControllerSwitchPosition.Center =>
                    Tr("Neutre"),

                GameControllerSwitchPosition.Up =>
                    Tr("Haut"),

                GameControllerSwitchPosition.UpRight =>
                    Tr("Haut + Droite"),

                GameControllerSwitchPosition.Right =>
                    Tr("Droite"),

                GameControllerSwitchPosition.DownRight =>
                    Tr("Bas + Droite"),

                GameControllerSwitchPosition.Down =>
                    Tr("Bas"),

                GameControllerSwitchPosition.DownLeft =>
                    Tr("Bas + Gauche"),

                GameControllerSwitchPosition.Left =>
                    Tr("Gauche"),

                GameControllerSwitchPosition.UpLeft =>
                    Tr("Haut + Gauche"),

                _ =>
                    switchValues[0].ToString()
            };
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : AFFICHAGE DU D-PAD
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : FERMETURE DU TESTEUR
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // Aucune configuration.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void TesterWindow_Closed(
        object? sender,
        EventArgs e)
    {
        reader.Dispose();
        inputTimer.Stop();

        inputTimer.Tick -=
            InputTimer_Tick;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : FERMETURE DU TESTEUR
    // ============================================================================

}
