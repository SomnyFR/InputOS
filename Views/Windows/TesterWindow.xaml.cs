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
    private readonly ControllerLocalProfile? localProfile;
    private readonly DualSenseMicrophoneReader? localMicrophone;

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
        localProfile = ControllerLocalProfileService.Load(controller);
        if (localProfile != null) localMicrophone = new DualSenseMicrophoneReader(controller);


        buttonValues =
            new bool[controller.ButtonCount];


        switchValues =
            new GameControllerSwitchPosition[controller.SwitchCount];


        axisValues =
            new double[controller.AxisCount];


        InitializeControllerInformation();

        CreateButtonIndicators();


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
        if (localProfile != null) Title = "InputOS - Profil local provisoire";
        ControllerNameText.Text =
            string.IsNullOrWhiteSpace(controller.DisplayName)
                ? "Manette"
                : controller.DisplayName;


        ControllerHardwareText.Text =
            $"VID : {controller.HardwareVendorId:X4} | " +
            $"PID : {controller.HardwareProductId:X4} | " +
            $"Boutons : {controller.ButtonCount} | " +
            $"Axes : {controller.AxisCount} | " +
            $"Switches : {controller.SwitchCount}";
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

    private const double ButtonWidth = 65;

    private const double ButtonHeight = 55;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void CreateButtonIndicators()
    {
        ButtonsPanel.Children.Clear();

        buttonIndicators.Clear();


        for (int i = 0; i < (localProfile?.Buttons.Mappings.Length ?? controller.ButtonCount); i++)
        {
            TextBlock buttonText = new()
            {
                Text = localProfile?.Buttons.Mappings[i].Name ?? $"B{i}",
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };


            Border buttonBorder = new()
            {
                Width = ButtonWidth,
                Height = ButtonHeight,

                Margin =
                    new Thickness(5),

                BorderThickness =
                    new Thickness(1),

                BorderBrush =
                    Brushes.Gray,

                Background =
                    Brushes.White,

                CornerRadius =
                    new CornerRadius(8),

                Child =
                    buttonText
            };


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
            controller.GetCurrentReading(
                buttonValues,
                switchValues,
                axisValues);


            UpdateButtons();

            UpdateAxes();

            UpdateSticks();

            UpdateDPad();
        }
        catch
        {
            inputTimer.Stop();

            Title =
                "InputOS - Manette déconnectée";
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

    private void UpdateButtons()
    {
        var reading = localProfile == null ? null : new ControllerDetectionResult(localProfile.Model,
            localProfile.VendorId, localProfile.ProductId, false, false, Array.Empty<string>(),
            buttonValues, axisValues, switchValues.Select(value => value.ToString()).ToArray())
            { MicrophonePressed = localMicrophone?.Pressed };
        for (int i = 0; i < buttonIndicators.Count; i++)
        {
            bool pressed = reading == null ? buttonValues[i] : ControllerButtonStateService.IsPressed(localProfile!.Buttons.Mappings[i], reading);
            Border indicator =
                buttonIndicators[i];


            indicator.Background =
                pressed
                    ? Brushes.LightGray
                    : Brushes.White;


            indicator.BorderBrush =
                pressed
                    ? Brushes.Black
                    : Brushes.Gray;


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
        if (localProfile != null)
            return new(localProfile.Joysticks.LeftX.AxisIndex, localProfile.Joysticks.LeftY.AxisIndex,
                localProfile.Joysticks.RightX.AxisIndex, localProfile.Joysticks.RightY.AxisIndex,
                localProfile.Buttons.Mappings.FirstOrDefault(m => m.Id == "LeftTrigger" && m.InputType == "Axis")?.InputIndex ?? -1,
                localProfile.Buttons.Mappings.FirstOrDefault(m => m.Id == "RightTrigger" && m.InputType == "Axis")?.InputIndex ?? -1);
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
        if (localProfile != null)
        {
            var axis = new[] { localProfile.Joysticks.LeftX, localProfile.Joysticks.LeftY,
                localProfile.Joysticks.RightX, localProfile.Joysticks.RightY }.First(a => a.AxisIndex == axisIndex);
            double value = axis.Normalize(rawValue);
            return axis == localProfile.Joysticks.LeftY || axis == localProfile.Joysticks.RightY ? -value : value;
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


        if (localProfile != null)
        {
            var mapping = localProfile.Buttons.Mappings.FirstOrDefault(m => m.InputType == "Axis" && m.InputIndex == axisIndex);
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
        if (localProfile != null)
        {
            var reading = new ControllerDetectionResult(localProfile.Model, localProfile.VendorId,
                localProfile.ProductId, false, false, Array.Empty<string>(), buttonValues, axisValues,
                switchValues.Select(value => value.ToString()).ToArray());
            var directions = localProfile.Buttons.Mappings.Where(m => m.Id.StartsWith("Dpad", StringComparison.Ordinal))
                .Where(m => ControllerButtonStateService.IsPressed(m, reading)).Select(m => m.Name).ToArray();
            DPadText.Text = directions.Length == 0 ? "Neutre" : string.Join(" + ", directions);
            return;
        }
        if (switchValues.Length == 0)
        {
            DPadText.Text =
                "Non disponible";

            return;
        }


        DPadText.Text =
            switchValues[0] switch
            {
                GameControllerSwitchPosition.Center =>
                    "Neutre",

                GameControllerSwitchPosition.Up =>
                    "Haut",

                GameControllerSwitchPosition.UpRight =>
                    "Haut + Droite",

                GameControllerSwitchPosition.Right =>
                    "Droite",

                GameControllerSwitchPosition.DownRight =>
                    "Bas + Droite",

                GameControllerSwitchPosition.Down =>
                    "Bas",

                GameControllerSwitchPosition.DownLeft =>
                    "Bas + Gauche",

                GameControllerSwitchPosition.Left =>
                    "Gauche",

                GameControllerSwitchPosition.UpLeft =>
                    "Haut + Gauche",

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
        localMicrophone?.Dispose();
        inputTimer.Stop();

        inputTimer.Tick -=
            InputTimer_Tick;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : FERMETURE DU TESTEUR
    // ============================================================================

}
