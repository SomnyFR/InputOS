using static InputOS.Services.LocalizationService;
using System.Windows;
using System.Windows.Controls;
using InputOS.Models;
using InputOS.Services;
using Windows.Gaming.Input;

namespace InputOS;

public partial class ControllerSuggestionWindow : Window
{

    // ============================================================================
    // FONCTIONNALITÉ : NOM DE LA CONFIGURATION MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private static readonly string OtherModel =
        Tr("Autre modèle");


    // Le choix du nom ne change pas la compatibilité du périphérique.
    private static readonly string[] controllerModels =
    [
        "DualShock 4",
        "DualSense",
        "DualSense Edge",
        "Xbox 360",
        "Xbox One",
        "Xbox Series S/X",
        "Xbox Elite",
        "Xbox Elite Series 2",
        "Nintendo Switch Pro",
        "Nintendo Switch Joy-Con",
        OtherModel
    ];


    public RawGameController SelectedController { get; }


    public ControllerConfigurationDraft? PendingConfiguration { get; private set; }


    public string? ConfigurationName =>
        PendingConfiguration?.Name;


    private readonly System.Windows.Threading.DispatcherTimer liveInputTimer =
        new()
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };


    private JoystickConfigurationService? joystickTest;

    private readonly Queue<double[]> joystickSamples = new();

    private System.Threading.CancellationTokenSource? detectionCancellation;

    private bool isClosed;
    private NintendoProControllerReader? nintendoReader;

    private ControllerButtonConfigurationService? buttonTest;
    private ControllerButtonDefinition[] buttonDefinitions = Array.Empty<ControllerButtonDefinition>();
    private readonly Queue<ControllerDetectionResult> buttonSamples = new();
    private readonly Dictionary<string, Border> buttonMarkers = new();


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public ControllerSuggestionWindow(
        RawGameController controller)
    {
        SelectedController =
            controller;


        InitializeComponent();


        liveInputTimer.Tick +=
            LiveInputTimer_Tick;


        Closed +=
            ControllerSuggestionWindow_Closed;


        ControllerInformationText.Text =
            Tf("Manette connectée : {0}\nVID : {1} | PID : {2}",
                ControllerCompatibilityService.GetControllerDisplayName(controller),
                controller.HardwareVendorId.ToString("X4"), controller.HardwareProductId.ToString("X4"));


        ControllerModelComboBox.ItemsSource =
            controllerModels
                .Where(model => model == OtherModel ||
                    !ControllerCompatibilityService.HasValidatedModel(model))
                .ToArray();
    }


    private void ControllerModelComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        CustomModelPanel.Visibility =
            ControllerModelComboBox.SelectedItem as string == OtherModel
                ? Visibility.Visible
                : Visibility.Collapsed;


        UpdateConfigurationName();
    }


    private void CustomModelTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        UpdateConfigurationName();
    }


    private void UpdateConfigurationName()
    {
        string? selectedModel =
            ControllerModelComboBox.SelectedItem as string;


        string? name =
            selectedModel == OtherModel
                ? CustomModelTextBox.Text.Trim()
                : selectedModel;


        ControllerDetectionResult? previousDetection =
            PendingConfiguration?.Detection;

        JoystickConfiguration? previousJoysticks =
            PendingConfiguration?.Joysticks;

        ControllerButtonConfiguration? previousButtons =
            PendingConfiguration?.Buttons;

        bool sameModel = string.Equals(PendingConfiguration?.Name, name, StringComparison.Ordinal);


        PendingConfiguration =
            string.IsNullOrWhiteSpace(name)
                ? null
                : new ControllerConfigurationDraft(
                    name,
                    SelectedController.HardwareVendorId,
                    SelectedController.HardwareProductId)
                {
                    Detection = previousDetection,
                    Joysticks = previousJoysticks,
                    Buttons = sameModel ? previousButtons : null
                };


        ConfigurationNameText.Text =
            ConfigurationName == null
                ? selectedModel == OtherModel
                    ? Tr("Saisissez le nom du modèle.")
                    : Tr("Sélectionnez un modèle.")
                : Tf("Nom de la configuration : {0}", ConfigurationName);


        NextButton.IsEnabled =
            PendingConfiguration != null;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : NOM DE LA CONFIGURATION MANETTE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : NAVIGATION DE LA CONFIGURATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void NextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (PendingConfiguration == null || !EnsureWiredConnection())
        {
            return;
        }


        ConfigurationSummaryText.Text =
            Tf("Configuration : {0}\nVID : {1} | PID : {2}\nBoutons : {3} | Axes : {4}",
                PendingConfiguration.Name,
                PendingConfiguration.VendorId.ToString("X4"), PendingConfiguration.ProductId.ToString("X4"),
                SelectedController.ButtonCount, SelectedController.AxisCount);


        ModelSelectionPanel.Visibility =
            Visibility.Collapsed;


        ConfigurationPreparationPanel.Visibility =
            Visibility.Visible;

        DetectionResultsPanel.Visibility =
            Visibility.Collapsed;

        ConfigurationScrollViewer.ScrollToTop();


        BackButton.Visibility =
            Visibility.Visible;


        NextButton.Visibility =
            Visibility.Collapsed;


    }


    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        liveInputTimer.Stop();

        StopJoystickTest();

        if (ButtonConfigurationPanel.Visibility == Visibility.Visible)
        {
            StopButtonTest();
            ButtonConfigurationPanel.Visibility = Visibility.Collapsed;
            JoystickConfigurationPanel.Visibility = Visibility.Visible;
            ConfigurationScrollViewer.ScrollToTop();
            if (PendingConfiguration?.Detection != null) liveInputTimer.Start();
            return;
        }

        if (JoystickConfigurationPanel.Visibility == Visibility.Visible)
        {
            JoystickConfigurationPanel.Visibility = Visibility.Collapsed;
            DetectionResultsPanel.Visibility = Visibility.Visible;
            ConfigurationScrollViewer.ScrollToTop();
            if (PendingConfiguration?.Detection != null)
            {
                liveInputTimer.Start();
            }
            return;
        }

        if (DetectionResultsPanel.Visibility == Visibility.Visible)
        {
            DetectionResultsPanel.Visibility = Visibility.Collapsed;
            ConfigurationPreparationPanel.Visibility = Visibility.Visible;
            ConfigurationScrollViewer.ScrollToTop();
            return;
        }


        ConfigurationPreparationPanel.Visibility =
            Visibility.Collapsed;


        ModelSelectionPanel.Visibility =
            Visibility.Visible;


        BackButton.Visibility =
            Visibility.Collapsed;


        NextButton.Visibility =
            Visibility.Visible;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : NAVIGATION DE LA CONFIGURATION
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : DÉTECTION AUTOMATIQUE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void LiveInputTimer_Tick(
        object? sender,
        EventArgs e)
    {
        ControllerDetectionResult? result =
            PendingConfiguration?.Detection;


        if (result == null)
        {
            liveInputTimer.Stop();
            return;
        }


        try
        {
            if (SelectedController.IsWireless ||
                !RawGameController.RawGameControllers.Contains(SelectedController))
            {
                throw new InvalidOperationException(Tr("Manette déconnectée."));
            }


            GameControllerSwitchPosition[] switches =
                new GameControllerSwitchPosition[result.SwitchPositions.Length];


            if (nintendoReader != null)
            {
                var reading = nintendoReader.Reading ?? throw new InvalidOperationException(Tr("Lecture Nintendo interrompue."));
                Array.Copy(reading.ButtonStates, result.ButtonStates, result.ButtonStates.Length);
                Array.Copy(reading.AxisValues, result.AxisValues, result.AxisValues.Length);
                for (int i = 0; i < switches.Length; i++)
                    switches[i] = Enum.Parse<GameControllerSwitchPosition>(reading.SwitchPositions[i]);
            }
            else
            {
                SelectedController.GetCurrentReading(result.ButtonStates, switches, result.AxisValues);
            }


            for (int i = 0; i < switches.Length; i++)
            {
                result.SwitchPositions[i] =
                    switches[i].ToString();
            }


            if (joystickTest != null)
            {
                joystickSamples.Enqueue((double[])result.AxisValues.Clone());
                // Conserver environ 300 ms de mesures malgré le rafraîchissement accru.
                while (joystickSamples.Count > 20)
                {
                    joystickSamples.Dequeue();
                }

                CaptureJoystickPositionButton.IsEnabled =
                    joystickSamples.Count >= 16;
            }


            if (ButtonConfigurationPanel.Visibility == Visibility.Visible)
            {
                UpdateButtonTestReading(result);
            }
            else if (JoystickConfigurationPanel.Visibility == Visibility.Visible)
            {
                // Actualiser seulement les points sur cette page, sans recréer les cartes masquées.
                UpdateJoystickVisual(result);
            }
            else
            {
                DisplayDetectionResult(result);
            }
        }
        catch (Exception)
        {
            liveInputTimer.Stop();


            ClearLiveCommands();


            if (PendingConfiguration != null)
            {
                PendingConfiguration =
                    PendingConfiguration with { Detection = null };
            }


            AutoDetectionResultText.Text =
                Tr("Lecture interrompue. Rebranchez la manette avec un câble USB. Si nécessaire, annulez et sélectionnez sa connexion filaire dans InputOS.");

            DetectionStatusText.Text = AutoDetectionResultText.Text;

            if (ButtonConfigurationPanel.Visibility == Visibility.Visible)
            {
                StopButtonTest();
                ButtonTestFeedbackText.Text = AutoDetectionResultText.Text;
            }
            else if (JoystickConfigurationPanel.Visibility == Visibility.Visible)
            {
                JoystickTestFeedbackText.Text = AutoDetectionResultText.Text;
            }
        }
    }


    private void DisplayDetectionResult(
        ControllerDetectionResult result)
    {
        UpdateJoystickVisual(result);

        LiveCommandsPanel.Visibility =
            Visibility.Visible;

        StartJoystickTestButton.IsEnabled =
            joystickTest == null && result.AxisValues.Length >= 4;


        AutoDetectionResultText.Text =
            Tf("Lecture en direct · {0} boutons · {1} axes\n", result.ButtonLabels.Length, result.AxisValues.Length) +
            (result.HasStandardGamepad
                ? Tr("Commandes standard reconnues par Windows.")
                : Tr("Certaines commandes devront être identifiées manuellement."));

        DetectionStatusText.Text = AutoDetectionResultText.Text;


        DetectedButtonsItems.ItemsSource =
            result.ButtonLabels.Select((label, index) => new
            {
                Name = Tf("Bouton {0}", index),
                Label = label == "Non identifié" ? Tr("Nom à identifier") : label,
                IsPressed = result.ButtonStates[index],
                State = result.ButtonStates[index] ? Tr("Appuyé") : Tr("Relâché")
            }).ToArray();


        DetectedAxesItems.ItemsSource =
            result.AxisValues.Select((rawValue, index) =>
            {
                JoystickAxisConfiguration? configuredAxis =
                    GetConfiguredJoystickAxis(index);

                bool isJoystick = configuredAxis != null ||
                    ControllerAutoDetectionService.IsJoystickAxis(
                        result.VendorId,
                        result.ProductId,
                        index);


                double value =
                    configuredAxis?.Normalize(rawValue) ??
                    (isJoystick ? (rawValue * 2.0) - 1.0 : rawValue);


                return new
                {
                    Name = isJoystick
                        ? Tf("Axe {0} · joystick", index)
                        : Tf("Axe {0} · valeur brute", index),
                    Minimum = isJoystick ? -1.0 : 0.0,
                    Value = value,
                    DisplayValue = value.ToString("0.000")
                };
            }).ToArray();


        DetectedSwitchesText.Text =
            result.SwitchPositions.Length == 0
                ? Tr("Aucune croix exposée séparément. Elle peut apparaître parmi les boutons.")
                : string.Join("\n", result.SwitchPositions.Select((position, index) =>
                    Tf("Direction {0} : {1}", index + 1, GetDirectionDisplayName(position))));
    }


    private static string GetDirectionDisplayName(
        string position)
    {
        return position switch
        {
            "Center" => Tr("Au repos"),
            "Up" => Tr("Haut"),
            "Down" => Tr("Bas"),
            "Left" => Tr("Gauche"),
            "Right" => Tr("Droite"),
            "UpLeft" => Tr("Haut gauche"),
            "UpRight" => Tr("Haut droite"),
            "DownLeft" => Tr("Bas gauche"),
            "DownRight" => Tr("Bas droite"),
            _ => position
        };
    }

    private async void AutoDetectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (detectionCancellation != null)
        {
            return;
        }

        liveInputTimer.Stop();


        ClearLiveCommands();


        if (PendingConfiguration == null || !EnsureWiredConnection())
        {
            return;
        }


        PendingConfiguration =
            PendingConfiguration with { Detection = null };

        using System.Threading.CancellationTokenSource cancellation = new();
        detectionCancellation = cancellation;
        System.Threading.CancellationToken token = cancellation.Token;
        ConfigurationPreparationPanel.Visibility = Visibility.Collapsed;
        DetectionResultsPanel.Visibility = Visibility.Collapsed;
        DetectionLoadingPanel.Visibility = Visibility.Visible;
        BackButton.IsEnabled = false;
        AutoDetectButton.IsEnabled = false;
        ConfigurationScrollViewer.ScrollToTop();

        try
        {
            if (nintendoReader != null) await nintendoReader.StopAsync();
            token.ThrowIfCancellationRequested();
            nintendoReader = NintendoProControllerReader.IsSupported(SelectedController)
                ? new NintendoProControllerReader(SelectedController) : null;
            NintendoProControllerReader? reader = nintendoReader;
            Task<ControllerDetectionResult> detection = Task.Run(
                () => reader != null ? reader.WaitForReading(token)
                    : ControllerAutoDetectionService.Detect(SelectedController), token);

            await Task.WhenAll(detection, Task.Delay(450, token));
            token.ThrowIfCancellationRequested();

            if (isClosed || PendingConfiguration == null || !EnsureWiredConnection())
            {
                return;
            }

            ControllerDetectionResult result = await detection;


            PendingConfiguration =
                PendingConfiguration with { Detection = result };


            DisplayDetectionResult(result);

            DetectionResultsPanel.Visibility = Visibility.Visible;
            ConfigurationScrollViewer.ScrollToTop();

            liveInputTimer.Start();
        }
        catch (OperationCanceledException)
        {
            // La fermeture annule la session sans restaurer ses données.
        }
        catch (Exception)
        {
            if (nintendoReader != null) await nintendoReader.StopAsync();
            nintendoReader = null;
            if (!isClosed)
            {
                if (PendingConfiguration != null)
                {
                    PendingConfiguration = PendingConfiguration with { Detection = null };
                }
                AutoDetectionResultText.Text =
                    Tr("Impossible de lire la manette. Rebranchez-la en USB et fermez les autres logiciels de manette, puis réessayez.");
            }
        }
        finally
        {
            detectionCancellation = null;
            if (!isClosed)
            {
                DetectionLoadingPanel.Visibility = Visibility.Collapsed;
                BackButton.IsEnabled = true;
                AutoDetectButton.IsEnabled = true;
                if (DetectionResultsPanel.Visibility != Visibility.Visible)
                {
                    ConfigurationPreparationPanel.Visibility = Visibility.Visible;
                }
            }
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : DÉTECTION AUTOMATIQUE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : CONNEXION FILAIRE REQUISE
    // ============================================================================

    private bool EnsureWiredConnection()
    {
        if (!SelectedController.IsWireless &&
            RawGameController.RawGameControllers.Contains(SelectedController))
        {
            return true;
        }


        liveInputTimer.Stop();


        ClearLiveCommands();


        if (PendingConfiguration != null)
        {
            PendingConfiguration =
                PendingConfiguration with { Detection = null };
        }


        AutoDetectionResultText.Text =
            Tr("La manette doit être connectée avec un câble USB pour poursuivre.");

        DetectionStatusText.Text = AutoDetectionResultText.Text;


        MessageBox.Show(
            this,
            Tr("Branchez la manette avec un câble USB. Si nécessaire, annulez et sélectionnez sa connexion filaire dans InputOS."),
            Tr("Connexion filaire requise"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);


        return false;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONNEXION FILAIRE REQUISE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : ANNULATION DE LA CONFIGURATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void ControllerSuggestionWindow_Closed(
        object? sender,
        System.EventArgs e)
    {
        isClosed = true;
        nintendoReader?.Dispose();
        diagnosticCancellation.Cancel();
        detectionCancellation?.Cancel();

        liveInputTimer.Stop();


        ClearLiveCommands();


        liveInputTimer.Tick -=
            LiveInputTimer_Tick;


        PendingConfiguration =
            null;


        ControllerModelComboBox.SelectedIndex =
            -1;


        CustomModelTextBox.Clear();


        ConfigurationSummaryText.Text =
            string.Empty;


        AutoDetectionResultText.Text =
            string.Empty;

        JoystickInstructionText.Text =
            string.Empty;

        JoystickTestFeedbackText.Text =
            string.Empty;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ANNULATION DE LA CONFIGURATION
    // ============================================================================

    // ============================================================================
    // FONCTIONNALITÉ : CONFIGURATION GUIDÉE DES JOYSTICKS
    // ============================================================================

    private JoystickAxisConfiguration? GetConfiguredJoystickAxis(int index)
    {
        JoystickConfiguration? configuration = PendingConfiguration?.Joysticks;
        if (configuration == null)
        {
            return null;
        }

        return new[] { configuration.LeftX, configuration.LeftY,
            configuration.RightX, configuration.RightY }
            .FirstOrDefault(axis => axis.AxisIndex == index);
    }


    private void StartJoystickTestButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingConfiguration?.Detection == null || !EnsureWiredConnection())
        {
            return;
        }

        PendingConfiguration = PendingConfiguration with { Joysticks = null, Buttons = null };
        ContinueToButtonsButton.IsEnabled = false;
        DetectionResultsPanel.Visibility = Visibility.Collapsed;
        JoystickConfigurationPanel.Visibility = Visibility.Visible;
        ConfigurationScrollViewer.ScrollToTop();
        RestartJoystickTestButton.Visibility = Visibility.Collapsed;
        joystickTest = new JoystickConfigurationService();
        joystickSamples.Clear();
        AutoDetectButton.IsEnabled = false;
        StartJoystickTestButton.IsEnabled = false;
        JoystickTestActions.Visibility = Visibility.Visible;
        CaptureJoystickPositionButton.IsEnabled = false;
        JoystickTestFeedbackText.Text = Tr("Maintenez chaque position jusqu’à la validation. Le test comporte deux positions au repos et huit directions.");
        JoystickInstructionText.Text = Tf("Étape 1 / 10 · {0}", joystickTest.Instruction);
        UpdateJoystickVisual(PendingConfiguration.Detection);
        liveInputTimer.Start();
    }


    private void CaptureJoystickPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (joystickTest == null || PendingConfiguration == null || !EnsureWiredConnection())
        {
            return;
        }

        try
        {
            joystickTest.Capture(joystickSamples.ToArray());
            joystickSamples.Clear();
            CaptureJoystickPositionButton.IsEnabled = false;

            if (joystickTest.IsComplete)
            {
                PendingConfiguration = PendingConfiguration with { Joysticks = joystickTest.Result };
                StopJoystickTest();
                JoystickTestFeedbackText.Text = Tr("Axes enregistrés. Bougez les deux joysticks : les points pleins suivent maintenant leurs positions. Le centre correspond à 0.");
                if (PendingConfiguration.Detection != null)
                {
                    DisplayDetectionResult(PendingConfiguration.Detection);
                }
                return;
            }

            JoystickInstructionText.Text = Tf("Étape {0} / 10 · {1}", joystickTest.Step + 1, joystickTest.Instruction);
            UpdateJoystickVisual(PendingConfiguration.Detection);
            JoystickTestFeedbackText.Text = Tr("Position validée. Passez à la direction suivante.");
        }
        catch (InvalidOperationException exception)
        {
            JoystickTestFeedbackText.Text = exception.Message;
            joystickSamples.Clear();
            CaptureJoystickPositionButton.IsEnabled = false;
        }
    }


    private void CancelJoystickTestButton_Click(object sender, RoutedEventArgs e)
    {
        StopJoystickTest();
        JoystickTestFeedbackText.Text = Tr("Test annulé. Les positions de ce test ont été effacées.");
        StartJoystickTestButton.IsEnabled = PendingConfiguration?.Detection != null;
    }


    private void StopJoystickTest()
    {
        joystickTest = null;
        joystickSamples.Clear();
        AutoDetectButton.IsEnabled = true;
        JoystickTestActions.Visibility = Visibility.Collapsed;
        CaptureJoystickPositionButton.IsEnabled = false;
        RestartJoystickTestButton.Visibility = Visibility.Visible;

        JoystickConfiguration? configuration = PendingConfiguration?.Joysticks;
        ContinueToButtonsButton.IsEnabled = configuration != null;
        JoystickInstructionText.Text = configuration == null
            ? Tr("Commencez le test après la détection de la manette.")
            : Tf("Joystick gauche : X = axe {0}, Y = axe {1}\nJoystick droit : X = axe {2}, Y = axe {3}",
                configuration.LeftX.AxisIndex, configuration.LeftY.AxisIndex,
                configuration.RightX.AxisIndex, configuration.RightY.AxisIndex);
        UpdateJoystickVisual(PendingConfiguration?.Detection);
    }

    private void UpdateJoystickVisual(ControllerDetectionResult? reading)
    {
        LeftJoystickTarget.Visibility = Visibility.Collapsed;
        RightJoystickTarget.Visibility = Visibility.Collapsed;
        LeftJoystickPosition.Visibility = Visibility.Collapsed;
        RightJoystickPosition.Visibility = Visibility.Collapsed;
        LeftJoystickDiagram.Opacity = 1;
        RightJoystickDiagram.Opacity = 1;

        if (joystickTest != null)
        {
            bool isLeft = joystickTest.Step < 5;
            int direction = joystickTest.Step % 5;
            double x = direction == 1 ? 100 : direction == 2 ? -100 : 0;
            double y = direction == 3 ? -100 : direction == 4 ? 100 : 0;
            FrameworkElement target = isLeft ? LeftJoystickTarget : RightJoystickTarget;
            target.Visibility = Visibility.Visible;
            Canvas.SetLeft(target, 120 + x - target.Width / 2);
            Canvas.SetTop(target, 120 + y - target.Height / 2);
            LeftJoystickDiagram.Opacity = isLeft ? 1 : 0.4;
            RightJoystickDiagram.Opacity = isLeft ? 0.4 : 1;
            return;
        }

        JoystickConfiguration? configuration = PendingConfiguration?.Joysticks;
        if (configuration == null || reading == null)
        {
            return;
        }

        MoveJoystickPoint(LeftJoystickPosition, configuration.LeftX, configuration.LeftY, reading);
        MoveJoystickPoint(RightJoystickPosition, configuration.RightX, configuration.RightY, reading);
    }

    private static void MoveJoystickPoint(FrameworkElement point,
        JoystickAxisConfiguration axisX, JoystickAxisConfiguration axisY,
        ControllerDetectionResult reading)
    {
        if (axisX.AxisIndex >= reading.AxisValues.Length || axisY.AxisIndex >= reading.AxisValues.Length)
        {
            return;
        }

        double x = axisX.Normalize(reading.AxisValues[axisX.AxisIndex]);
        double y = axisY.Normalize(reading.AxisValues[axisY.AxisIndex]);
        double distance = Math.Sqrt(x * x + y * y);
        if (distance > 1)
        {
            x /= distance;
            y /= distance;
        }
        point.Visibility = Visibility.Visible;
        Canvas.SetLeft(point, 120 + x * 100 - point.Width / 2);
        Canvas.SetTop(point, 120 - y * 100 - point.Height / 2);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONFIGURATION GUIDÉE DES JOYSTICKS
    // ============================================================================


    private void ClearLiveCommands()
    {
        StopButtonTest();

        StopJoystickTest();

        StartJoystickTestButton.IsEnabled =
            false;

        LiveCommandsPanel.Visibility =
            Visibility.Collapsed;


        DetectedButtonsItems.ItemsSource =
            null;


        DetectedAxesItems.ItemsSource =
            null;


        DetectedSwitchesText.Text =
            string.Empty;
    }

}
