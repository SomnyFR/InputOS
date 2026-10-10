using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InputOS.Models;
using InputOS.Services;

namespace InputOS;

public partial class ControllerSuggestionWindow
{
    private DualSenseMicrophoneReader? microphoneReader;
    private readonly HashSet<string> livePressedButtons = new();
    // ============================================================================
    // FONCTIONNALITÉ : PAGE VISUELLE DE CONFIGURATION DES BOUTONS
    // ============================================================================

    private void ContinueToButtonsButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingConfiguration?.Joysticks == null ||
            PendingConfiguration.Detection == null || !EnsureWiredConnection()) return;

        StopJoystickTest();
        StopButtonTest();
        JoystickConfigurationPanel.Visibility = Visibility.Collapsed;
        ButtonConfigurationPanel.Visibility = Visibility.Visible;
        buttonDefinitions = ControllerButtonCatalogService.GetButtons(PendingConfiguration.Name);
        ButtonModelText.Text = $"{PendingConfiguration.Name} · {buttonDefinitions.Length} commandes à identifier, gâchettes et clics des sticks inclus.";
        if (ControllerButtonCatalogService.GetFamily(PendingConfiguration.Name) == "Generic")
            ButtonModelText.Text += " Le modèle personnalisé utilise une disposition générique ; signalez les commandes absentes.";
        if (PendingConfiguration.Name == "Nintendo Switch Joy-Con")
            ButtonModelText.Text += " Le schéma représente une paire de Joy-Con. Les commandes disponibles dépendent du périphérique présenté par Windows.";
        DrawControllerButtons();
        BeginButtonTestButton.Visibility = Visibility.Visible;
        ButtonInstructionText.Text = "Relâchez toutes les commandes, puis commencez le test.";
        ButtonTestFeedbackText.Text = "Chaque commande sera mise en évidence à tour de rôle. Maintenez-la un court instant : l’appui sera enregistré automatiquement. Relâchez-la avant la suivante. Pour L3 et R3, enfoncez le joystick.";
        RefreshButtonChecklist();
        if (PendingConfiguration.Buttons != null) StartButtonPreview();
        ConfigurationScrollViewer.ScrollToTop();
        liveInputTimer.Start();
    }

    private void BeginButtonTestButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingConfiguration?.Joysticks == null || !EnsureWiredConnection()) return;
        try
        {
            ControllerDetectionResult rest = ControllerAutoDetectionService.Detect(SelectedController);
            var sticks = PendingConfiguration.Joysticks;
            if (rest.ButtonStates.Any(value => value) || rest.SwitchPositions.Any(value => value != "Center") ||
                new[] { sticks.LeftX, sticks.LeftY, sticks.RightX, sticks.RightY }
                    .Any(axis => Math.Abs(axis.Normalize(rest.AxisValues[axis.AxisIndex])) > 0.2))
            {
                ButtonTestFeedbackText.Text = "Relâchez les boutons, les sticks et les gâchettes avant de commencer.";
                return;
            }
            PendingConfiguration = PendingConfiguration with { Buttons = null, Detection = rest };
            ProfileExportPanel.Visibility = Visibility.Collapsed;
            ProfileExportFeedbackText.Text = "L’envoi se fait directement depuis InputOS. Le profil local et l’envoi du diagnostic sont indépendants.";
            livePressedButtons.Clear();
            buttonTest = new ControllerButtonConfigurationService(PendingConfiguration.Name, rest.AxisValues, sticks);
            microphoneReader?.Dispose();
            microphoneReader = new DualSenseMicrophoneReader(SelectedController);
            buttonSamples.Clear();
            BeginButtonTestButton.Visibility = Visibility.Collapsed;
            ButtonTestActions.Visibility = Visibility.Visible;
            SaveButtonConfigurationButton.Visibility = Visibility.Collapsed;
            UpdateButtonTestInstruction();
            RefreshButtonChecklist();
            liveInputTimer.Start();
        }
        catch (Exception)
        {
            ButtonTestFeedbackText.Text = "Impossible de commencer. Vérifiez la connexion USB et recommencez la détection si nécessaire.";
        }
    }

    private void UpdateButtonTestReading(ControllerDetectionResult reading)
    {
        reading = reading with { MicrophonePressed = microphoneReader?.Pressed };
        if (buttonTest == null)
        {
            UpdateButtonPreview(reading);
            return;
        }
        bool wasWaiting = buttonTest.WaitingForRelease;
        buttonTest.ObserveRelease(reading);
        if (wasWaiting) buttonSamples.Clear();
        if (!buttonTest.WaitingForRelease && !buttonTest.IsComplete && !buttonTest.IsAtRest(reading))
        {
            buttonSamples.Enqueue(reading with
            {
                ButtonStates = (bool[])reading.ButtonStates.Clone(),
                AxisValues = (double[])reading.AxisValues.Clone(),
                SwitchPositions = (string[])reading.SwitchPositions.Clone()
            });
            while (buttonSamples.Count > 12) buttonSamples.Dequeue();
            if (buttonSamples.Count >= 8)
            {
                try
                {
                    buttonTest.Capture(buttonSamples.ToArray());
                    buttonSamples.Clear();
                    UpdateButtonTestInstruction();
                    RefreshButtonChecklist();
                }
                catch (InvalidOperationException exception)
                {
                    ButtonTestFeedbackText.Text = exception.Message;
                    buttonSamples.Clear();
                }
            }
        }
        else buttonSamples.Clear();
        SkipButtonInputButton.IsEnabled = !buttonTest.WaitingForRelease;
        SaveButtonConfigurationButton.IsEnabled = buttonTest.IsComplete && !buttonTest.WaitingForRelease;
        if (wasWaiting && !buttonTest.WaitingForRelease) UpdateButtonTestInstruction();
    }

    private void SkipButtonInputButton_Click(object sender, RoutedEventArgs e)
    {
        if (buttonTest == null || buttonTest.WaitingForRelease || !EnsureWiredConnection()) return;
        buttonTest.Skip();
        buttonSamples.Clear();
        UpdateButtonTestInstruction();
        RefreshButtonChecklist();
    }

    private void SaveButtonConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
        if (buttonTest == null || !buttonTest.IsComplete || buttonTest.WaitingForRelease ||
            PendingConfiguration == null || !EnsureWiredConnection()) return;

        PendingConfiguration = PendingConfiguration with { Buttons = buttonTest.GetResult() };
        ButtonInstructionText.Text = "Les associations sont enregistrées pour cette configuration.";
        ButtonTestFeedbackText.Text = $"{PendingConfiguration.Buttons.Mappings.Length} commandes associées ; " +
            $"{PendingConfiguration.Buttons.UnavailableButtons.Length} non détectées. Ces données restent temporaires et seront effacées si vous annulez la configuration.";
        StopButtonTest();
        BeginButtonTestButton.Content = "Recommencer le test des boutons";
        BeginButtonTestButton.Visibility = Visibility.Visible;
        RefreshButtonChecklist();
        StartButtonPreview();
    }

    private void StartButtonPreview()
    {
        ProfileExportPanel.Visibility = Visibility.Visible;
        microphoneReader?.Dispose();
        microphoneReader = new DualSenseMicrophoneReader(SelectedController);
        ButtonInstructionText.Text = "Test en direct · Appuyez sur les commandes de votre manette.";
        ButtonTestFeedbackText.Text = "Les commandes associées s’allument pendant l’appui. Les associations restent temporaires jusqu’à la fin de la configuration.";
        liveInputTimer.Start();
    }

    private void UpdateButtonPreview(ControllerDetectionResult reading)
    {
        if (PendingConfiguration?.Buttons == null) return;
        var pressed = PendingConfiguration.Buttons.Mappings
            .Where(mapping => ControllerButtonStateService.IsPressed(mapping, reading))
            .Select(mapping => mapping.Id).ToHashSet();
        if (livePressedButtons.SetEquals(pressed)) return;
        livePressedButtons.Clear();
        livePressedButtons.UnionWith(pressed);
        RefreshButtonChecklist();
        var names = PendingConfiguration.Buttons.Mappings
            .Where(mapping => pressed.Contains(mapping.Id)).Select(mapping => mapping.Name);
        ButtonTestFeedbackText.Text = pressed.Count == 0
            ? "Aucune commande appuyée. Appuyez sur un bouton, une direction ou une gâchette pour vérifier son association."
            : $"Appui détecté : {string.Join(" · ", names)}";
    }

    private void UpdateButtonTestInstruction()
    {
        if (buttonTest == null) return;
        SkipButtonInputButton.IsEnabled = !buttonTest.WaitingForRelease;
        ButtonTestActions.Visibility = buttonTest.IsComplete ? Visibility.Collapsed : Visibility.Visible;
        SaveButtonConfigurationButton.Visibility = buttonTest.IsComplete ? Visibility.Visible : Visibility.Collapsed;
        SaveButtonConfigurationButton.IsEnabled = buttonTest.IsComplete && !buttonTest.WaitingForRelease;
        ButtonInstructionText.Text = buttonTest.IsComplete
            ? "Toutes les commandes ont été parcourues."
            : $"Commande {buttonTest.Step + 1} / {buttonTest.Definitions.Length} · Appuyez sur {buttonTest.Current!.Name}";
        ButtonTestFeedbackText.Text = buttonTest.WaitingForRelease
            ? "Relâchez la commande enregistrée avant de poursuivre."
            : buttonTest.IsComplete ? "Cliquez sur « Enregistrer les boutons » pour conserver les associations."
            : buttonTest.Current!.IsTrigger
                ? "Enfoncez complètement cette gâchette et maintenez-la un court instant. L’appui sera enregistré automatiquement."
                : "Maintenez uniquement cette commande un court instant. L’appui sera enregistré automatiquement ; une commande déjà associée ne peut pas être réutilisée.";
        HighlightCurrentButton();
    }

    private void RefreshButtonChecklist()
    {
        var mappings = buttonTest?.Mappings.ToArray() ?? PendingConfiguration?.Buttons?.Mappings ?? Array.Empty<ControllerButtonMapping>();
        var unavailable = buttonTest?.UnavailableButtons.ToArray() ?? PendingConfiguration?.Buttons?.UnavailableButtons ?? Array.Empty<string>();
        ButtonChecklistItems.ItemsSource = buttonDefinitions.Select(definition => new
        {
            definition.Name,
            Status = livePressedButtons.Contains(definition.Id) ? "● Appuyée"
                : mappings.Any(mapping => mapping.Id == definition.Id) ? "✓ Associée"
                : unavailable.Contains(definition.Name) ? "Non détectée / absente"
                : buttonTest?.Current?.Id == definition.Id ? "À tester maintenant" : "À identifier"
        }).ToArray();
        HighlightCurrentButton();
    }

    private void StopButtonTest()
    {
        ProfileExportPanel.Visibility = Visibility.Collapsed;
        livePressedButtons.Clear();
        microphoneReader?.Dispose();
        microphoneReader = null;
        buttonTest = null;
        buttonSamples.Clear();
        ButtonTestActions.Visibility = Visibility.Collapsed;
        SaveButtonConfigurationButton.Visibility = Visibility.Collapsed;
        SaveButtonConfigurationButton.IsEnabled = false;
        HighlightCurrentButton();
    }

    private void DrawControllerButtons()
    {
        buttonMarkers.Clear();
        foreach (var marker in ControllerDiagramService.Draw(
            ControllerButtonDiagram, PendingConfiguration!.Name, buttonDefinitions))
        {
            buttonMarkers.Add(marker.Key, marker.Value);
        }
        HighlightCurrentButton();
    }

    private void HighlightCurrentButton()
    {
        foreach (var pair in buttonMarkers)
        {
            bool active = buttonTest?.Current?.Id == pair.Key || livePressedButtons.Contains(pair.Key);
            pair.Value.SetResourceReference(Border.BackgroundProperty, active ? "AppAccentBrush" : "AppPanelBrush");
            pair.Value.SetResourceReference(Border.BorderBrushProperty, active ? "AppAccentBrush" : "AppForegroundBrush");
            var label = (TextBlock)pair.Value.Child;
            if (active) label.Foreground = Brushes.White;
            else label.SetResourceReference(TextBlock.ForegroundProperty, "AppForegroundBrush");
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : PAGE VISUELLE DE CONFIGURATION DES BOUTONS
    // ============================================================================
}
