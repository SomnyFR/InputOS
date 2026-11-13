using static InputOS.Services.LocalizationService;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using InputOS.Models;
using InputOS.Services;
using Windows.Gaming.Input;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Brush = System.Windows.Media.Brush;

namespace InputOS;

public partial class OtherControllerWindow : Window
{
    // ============================================================================
    // FONCTIONNALITÉ : CONFIGURATION LIBRE (MATÉRIEL NON LISTÉ)
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private const double MovementThreshold = 0.15;

    private static readonly (string Label, string Value)[] TypeChoices =
    [
        ("Manette de jeu", "Gamepad"), ("Stick arcade", "ArcadeStick"), ("Volant", "Wheel"),
        ("Joystick ou HOTAS", "Joystick"), ("Radiocommande ou drone", "RadioController"), ("Autre", "Other")
    ];

    private static readonly (string Label, string Value)[] AxisTypeChoices =
    [
        ("Stick (centre au repos)", "Stick"), ("Gâchette", "Trigger"), ("Gaz sans centre", "Throttle"),
        ("Curseur", "Slider"), ("Potentiomètre", "Dial")
    ];

    // Noms proposés selon le type ; la saisie libre reste toujours possible.
    private static readonly Dictionary<string, string[]> Suggestions = new()
    {
        ["Gamepad"] = ["A", "B", "X", "Y", "LB", "RB", "LT", "RT", "Start", "Select", "L3", "R3", "Left stick X", "Left stick Y", "Right stick X", "Right stick Y"],
        ["ArcadeStick"] = ["Up", "Down", "Left", "Right", "Button 1", "Button 2", "Button 3", "Button 4", "Button 5", "Button 6", "Start", "Select"],
        ["Wheel"] = ["Steering", "Throttle", "Brake", "Clutch", "Paddle left", "Paddle right", "Shifter up", "Shifter down", "Handbrake"],
        ["Joystick"] = ["X", "Y", "Twist", "Throttle", "Trigger", "Fire", "Hat", "Rudder", "Button 1", "Button 2", "Button 3", "Button 4"],
        ["RadioController"] = ["Roll", "Pitch", "Yaw", "Throttle", "Switch A", "Switch B", "Switch C", "Switch D",
            "Trim roll", "Trim pitch", "Trim yaw", "Trim throttle", "Knob 1", "Knob 2"],
        ["Other"] = []
    };

    private sealed class InputRow
    {
        public string Kind = "";
        public int Index;
        public Border Card = new();
        public TextBlock Live = new();
        public ComboBox NameBox = new(), Status = new();
        public ComboBox? TypeBox;
        public TextBox? Min, Center, Max, Dead;
        public CheckBox? NoCenter, Invert;
        public double Rest, Low, High;
        public bool Manual;

        public string Label => Kind switch
        {
            "Button" => Tf("Bouton {0}", Index),
            "Axis" => Tf("Axe {0}", Index),
            _ => Tf("Croix {0}", Index)
        };

        public string Name => NameBox.Text.Trim();
        public bool IsNamed => Status.SelectedIndex == 0 && Name.Length > 0;
        public bool IsAbsent => Status.SelectedIndex == 2;
    }

    private readonly RawGameController controller;
    private readonly string connection;
    private readonly bool wireless;
    private readonly int buttonCount, axisCount, switchCount;
    private readonly List<InputRow> rows = [];
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    // Lecteur choisi par ControllerReaderFactory (lecture Windows brute pour tout matériel inconnu).
    private IControllerInputReader reader;
    private long readerStartedAt = Environment.TickCount64;

    private readonly bool[] buttons;
    private readonly GameControllerSwitchPosition[] switches;
    private readonly double[] axes;
    private bool[]? baseButtons;
    private double[]? baseAxes;
    private bool updatingText;
    private bool ready;
    private InputRow? highlighted;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public OtherControllerWindow(RawGameController selectedController)
    {
        controller = selectedController;
        connection = ControllerLocalProfileService.GetConnection(controller);
        wireless = controller.IsWireless;
        buttonCount = controller.ButtonCount;
        axisCount = controller.AxisCount;
        switchCount = controller.SwitchCount;
        reader = ControllerReaderFactory.Create(controller, out string? readerWarning);
        buttons = new bool[buttonCount];
        switches = new GameControllerSwitchPosition[switchCount];
        axes = new double[axisCount];

        InitializeComponent();

        ControllerInformationText.Text = Tf("Matériel connecté : {0}\nVID : {1} | PID : {2} | {3}\nBoutons : {4} | Axes : {5} | Switchs : {6}",
            ControllerCompatibilityService.GetControllerDisplayName(controller),
            controller.HardwareVendorId.ToString("X4"), controller.HardwareProductId.ToString("X4"), connection,
            buttonCount, axisCount, switchCount);
        ProfileNameBox.Text = Clean(ControllerCompatibilityService.GetControllerDisplayName(controller));
        ControllerTypeBox.ItemsSource = TypeChoices.Select(t => Tr(t.Label)).ToArray();
        ControllerTypeBox.SelectedIndex = TypeChoices.Length - 1;
        ButtonCountBox.Text = buttonCount.ToString();
        AxisCountBox.Text = axisCount.ToString();
        SwitchCountBox.Text = switchCount.ToString();
        CountHintText.Text = Tr("Valeurs détectées par Windows. Réduisez-les pour ne configurer que les premières entrées ; les autres seront ignorées.");

        for (int i = 0; i < buttonCount; i++) AddRow("Button", i);
        for (int i = 0; i < axisCount; i++) AddRow("Axis", i);
        for (int i = 0; i < switchCount; i++) AddRow("Switch", i);
        ready = true;
        ApplyCounts();
        LastInputText.Text = Tr("Bougez une commande de votre matériel pour la repérer.");

        timer.Tick += Timer_Tick;
        timer.Start();
        if (readerWarning != null) ShowMessage(readerWarning);
        Closed += (_, _) => { timer.Stop(); reader.Dispose(); };
    }

    // Retire les caractères que le service de diagnostics refuse.
    private static string Clean(string text) =>
        new string(text.Where(c => c is not ('`' or '＠' or '<' or '>' or '\r' or '\n')).ToArray()).Trim();

    private string SelectedType => TypeChoices[Math.Max(0, ControllerTypeBox.SelectedIndex)].Value;

    private void AddRow(string kind, int index)
    {
        InputRow row = new() { Kind = kind, Index = index };
        StackPanel body = new();
        Grid line = new();
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        TextBlock label = new() { Text = row.Label, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        row.Live.VerticalAlignment = VerticalAlignment.Center;
        row.Live.Foreground = (Brush)FindResource("AppSecondaryTextBrush");
        row.NameBox.IsEditable = true;
        row.NameBox.Margin = new Thickness(0, 0, 8, 0);
        row.NameBox.ToolTip = Tr("Nom de l’entrée : choisissez dans la liste ou saisissez-le (100 caractères maximum).");
        row.Status.ItemsSource = new[] { Tr("À nommer"), Tr("Ignorée"), Tr("Absente") };
        row.Status.SelectedIndex = 0;
        Grid.SetColumn(row.Live, 1);
        Grid.SetColumn(row.NameBox, 2);
        Grid.SetColumn(row.Status, 3);
        foreach (UIElement element in new UIElement[] { label, row.Live, row.NameBox, row.Status }) line.Children.Add(element);
        body.Children.Add(line);

        if (kind == "Axis")
        {
            WrapPanel options = new() { Margin = new Thickness(0, 8, 0, 0) };
            row.TypeBox = new ComboBox
            {
                ItemsSource = AxisTypeChoices.Select(t => Tr(t.Label)).ToArray(),
                SelectedIndex = 3,
                Width = 190,
                Margin = new Thickness(0, 0, 12, 0)
            };
            row.TypeBox.SelectionChanged += (_, _) =>
            {
                if (row.NoCenter != null)
                    row.NoCenter.IsChecked = AxisTypeChoices[Math.Max(0, row.TypeBox.SelectedIndex)].Value == "Throttle";
            };
            row.Min = Field(row, 60);
            row.Center = Field(row, 60);
            row.Max = Field(row, 60);
            row.Dead = new TextBox { Width = 48, Text = "0", Margin = new Thickness(0, 0, 12, 0) };
            row.NoCenter = new CheckBox { Content = Tr("Sans centre"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            row.Invert = new CheckBox { Content = Tr("Inverser"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            foreach ((string text, UIElement control) in new (string, UIElement)[]
            {
                ("", row.TypeBox), ("Min", row.Min), ("Centre", row.Center), ("Max", row.Max),
                ("", row.NoCenter), ("", row.Invert), ("Zone morte", row.Dead)
            })
            {
                if (text != "")
                    options.Children.Add(new TextBlock { Text = Tr(text), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
                options.Children.Add(control);
            }
            body.Children.Add(options);
        }

        row.Card.Padding = new Thickness(12);
        row.Card.Margin = new Thickness(0, 0, 0, 8);
        row.Card.CornerRadius = new CornerRadius(10);
        row.Card.Background = (Brush)FindResource("AppCardBrush");
        row.Card.BorderBrush = (Brush)FindResource("AppBorderBrush");
        row.Card.BorderThickness = new Thickness(1);
        row.Card.Child = body;
        rows.Add(row);
        EntriesPanel.Children.Add(row.Card);
        SetSuggestions(row);
    }

    private TextBox Field(InputRow row, double width)
    {
        TextBox box = new() { Width = width, Margin = new Thickness(0, 0, 12, 0) };
        box.TextChanged += (_, _) => { if (!updatingText) row.Manual = true; };
        return box;
    }

    private void SetSuggestions(InputRow row)
    {
        string text = row.NameBox.Text;
        row.NameBox.ItemsSource = Suggestions[SelectedType];
        row.NameBox.Text = text;
    }

    private void ControllerTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ready) foreach (InputRow row in rows) SetSuggestions(row);
    }

    private void CountBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ready) ApplyCounts();
    }

    private static int ParseCount(TextBox box, int maximum) =>
        int.TryParse(box.Text.Trim(), out int value) ? Math.Clamp(value, 0, maximum) : maximum;

    private void ApplyCounts()
    {
        int b = ParseCount(ButtonCountBox, buttonCount), a = ParseCount(AxisCountBox, axisCount), s = ParseCount(SwitchCountBox, switchCount);
        foreach (InputRow row in rows)
            row.Card.Visibility = row.Index < (row.Kind == "Button" ? b : row.Kind == "Axis" ? a : s)
                ? Visibility.Visible : Visibility.Collapsed;
    }

    // Le message reste visible : la page défile jusqu'à lui et, pour un échec d'enregistrement, un dialogue s'ouvre aussi.
    private void ShowMessage(string text, bool dialog = false)
    {
        MessageText.Text = text;
        MessageCard.Visibility = Visibility.Visible;
        MessageCard.BringIntoView();
        if (dialog)
            MessageBox.Show(this, text, Tr("InputOS - Configuration libre"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ---- Détection en direct ----

    private void ResetDetectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RawGameController.RawGameControllers.Contains(controller))
        {
            ShowMessage(Tr("Le matériel est déconnecté. Reconnectez-le puis réessayez ; vos réglages sont conservés."));
            return;
        }
        MessageCard.Visibility = Visibility.Collapsed;
        baseButtons = null;
        baseAxes = null;
        timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (!RawGameController.RawGameControllers.Contains(controller)) throw new InvalidOperationException();
            if (reader.Read() is not { } reading)
            {
                if (Environment.TickCount64 - readerStartedAt < 4500) return;
                // Lecteur spécifique muet : repli sur la lecture Windows brute, signalé à l'utilisateur.
                reader.Dispose();
                reader = ControllerReaderFactory.CreateRaw(controller);
                ShowMessage(ControllerReaderFactory.FallbackMessage);
                return;
            }
            Array.Copy(reading.ButtonStates, buttons, Math.Min(reading.ButtonStates.Length, buttons.Length));
            Array.Copy(reading.AxisValues, axes, Math.Min(reading.AxisValues.Length, axes.Length));
            for (int i = 0; i < switches.Length && i < reading.SwitchPositions.Length; i++)
                switches[i] = Enum.Parse<GameControllerSwitchPosition>(reading.SwitchPositions[i]);
        }
        catch (Exception)
        {
            timer.Stop();
            LastInputText.Text = Tr("Lecture interrompue.");
            ShowMessage(Tr("Le matériel a été déconnecté. Vos réglages sont conservés : vous pouvez les modifier et enregistrer, ou reconnecter le matériel puis cliquer sur « Réinitialiser la détection »."));
            return;
        }

        if (baseButtons == null || baseAxes == null)
        {
            baseButtons = (bool[])buttons.Clone();
            baseAxes = (double[])axes.Clone();
            foreach (InputRow axisRow in rows.Where(r => r.Kind == "Axis" && !r.Manual))
            {
                axisRow.Rest = axisRow.Low = axisRow.High = axes[axisRow.Index];
                ShowRange(axisRow);
            }
        }

        InputRow? hit = null;
        string value = "";
        double bestMove = 0;
        foreach (InputRow row in rows)
        {
            bool visible = row.Card.Visibility == Visibility.Visible;
            switch (row.Kind)
            {
                case "Button":
                    row.Live.Text = buttons[row.Index] ? Tr("Appuyé") : Tr("Relâché");
                    if (visible && buttons[row.Index] && !baseButtons[row.Index] && bestMove < 10)
                    {
                        hit = row; value = Tr("Appuyé"); bestMove = 10;
                    }
                    break;
                case "Switch":
                    row.Live.Text = switches[row.Index].ToString();
                    if (visible && switches[row.Index] != GameControllerSwitchPosition.Center && bestMove < 10)
                    {
                        hit = row; value = switches[row.Index].ToString(); bestMove = 10;
                    }
                    break;
                default:
                    double v = axes[row.Index];
                    row.Live.Text = v.ToString("0.000", CultureInfo.InvariantCulture);
                    if (!row.Manual)
                    {
                        bool changed = v < row.Low || v > row.High;
                        row.Low = Math.Min(row.Low, v);
                        row.High = Math.Max(row.High, v);
                        if (changed) ShowRange(row);
                    }
                    double move = Math.Abs(v - baseAxes[row.Index]);
                    if (visible && move > MovementThreshold && move > bestMove)
                    {
                        hit = row; value = v.ToString("0.000", CultureInfo.InvariantCulture); bestMove = move;
                    }
                    break;
            }
        }

        if (hit == null) return;
        string text = Tf("Entrée détectée : {0} · valeur brute {1}", hit.Label, value);
        if (hit.IsNamed) text += "\n" + Tf("Déjà nommée « {0} » : modifiez le nom pour la renommer.", hit.Name);
        LastInputText.Text = text;
        if (!ReferenceEquals(highlighted, hit))
        {
            if (highlighted != null) highlighted.Card.BorderBrush = (Brush)FindResource("AppBorderBrush");
            hit.Card.BorderBrush = (Brush)FindResource("AppAccentBrush");
            highlighted = hit;
            hit.Card.BringIntoView();
        }
    }

    private void ShowRange(InputRow row)
    {
        updatingText = true;
        row.Min!.Text = Number(row.Low);
        row.Max!.Text = Number(row.High);
        row.Center!.Text = Number(row.Rest);
        updatingText = false;
    }

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static bool TryNumber(TextBox? box, out double value)
    {
        value = 0;
        return box != null && double.TryParse(box.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            double.IsFinite(value);
    }

    // ---- Enregistrement ----

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ControllerConfigurationDraft? draft = BuildDraft(out string? error);
            if (draft == null)
            {
                ShowMessage(error!, true);
                return;
            }
            string path = ControllerProfileExportService.SaveToProfilesDirectory(draft);
            MessageBox.Show(this, Tf("Profil enregistré : {0}\nIl est utilisable dès maintenant dans le testeur d’InputOS.", path),
                Tr("InputOS - Configuration libre"), MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception)
        {
            ShowMessage(Tr("Impossible d’enregistrer le profil. Vérifiez l’accès à Documents\\InputOS\\profils et réessayez."), true);
        }
    }

    private ControllerConfigurationDraft? BuildDraft(out string? error)
    {
        error = null;
        string name = ProfileNameBox.Text.Trim();
        if (!ControllerLocalProfileService.IsValidName(name))
        {
            error = Tr("Le nom du matériel est vide ou invalide (100 caractères maximum, sans retour à la ligne ni les caractères ` ＠ < >).");
            return null;
        }

        var visible = rows.Where(r => r.Card.Visibility == Visibility.Visible).ToList();
        var named = visible.Where(r => r.IsNamed).ToList();
        if (named.Count == 0)
        {
            error = Tr("Nommez au moins une entrée avant d’enregistrer : bougez une commande, puis saisissez son nom.");
            return null;
        }
        foreach (InputRow row in named)
        {
            if (!ControllerLocalProfileService.IsValidName(row.Name))
            {
                error = Tf("{0} : nom invalide (100 caractères maximum, sans retour à la ligne ni les caractères ` ＠ < >).", row.Label);
                return null;
            }
        }
        var duplicate = named.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            error = Tf("Le nom « {0} » est utilisé par plusieurs entrées ({1}). Chaque entrée doit avoir un nom différent.",
                duplicate.Key, string.Join(", ", duplicate.Select(r => r.Label)));
            return null;
        }

        List<JoystickAxisConfiguration> sticks = [], others = [];
        foreach (InputRow row in named.Where(r => r.Kind == "Axis"))
        {
            JoystickAxisConfiguration? axis = BuildAxis(row, out error);
            if (axis == null) return null;
            (axis.Type == "Stick" ? sticks : others).Add(axis);
        }
        if (sticks.Count is not (0 or 2 or 4))
        {
            error = Tr("Les axes de type « Stick » vont par paires X/Y : il en faut 2 ou 4. Changez le type de certains axes.");
            return null;
        }
        sticks = sticks.OrderBy(a => a.AxisIndex).ToList();
        JoystickConfiguration joysticks = new(sticks.ElementAtOrDefault(0), sticks.ElementAtOrDefault(1),
            sticks.ElementAtOrDefault(2), sticks.ElementAtOrDefault(3)) { Axes = [.. others] };

        List<ControllerButtonMapping> mappings = [];
        foreach (InputRow row in named.Where(r => r.Kind == "Button"))
            mappings.Add(new ControllerButtonMapping($"Button{row.Index}", row.Name, "Button", row.Index));
        foreach (InputRow row in named.Where(r => r.Kind == "Switch"))
            foreach (string position in new[] { "Up", "Down", "Left", "Right" })
                mappings.Add(new ControllerButtonMapping($"Switch{row.Index}{position}", $"{row.Name} {position}", "Switch", row.Index, position));
        string[] unavailable = visible.Where(r => r.IsAbsent).Select(r => r.Label).ToArray();

        ControllerDetectionResult detection = new(name, controller.HardwareVendorId, controller.HardwareProductId, wireless, false,
            new string[buttonCount], new bool[buttonCount], new double[axisCount], new string[switchCount])
            { InputReadingFormat = reader.InputReadingFormat };
        ControllerConfigurationDraft draft = new(name, controller.HardwareVendorId, controller.HardwareProductId)
        {
            Connection = connection,
            ControllerType = SelectedType,
            Detection = detection,
            Joysticks = joysticks,
            Buttons = new ControllerButtonConfiguration([.. mappings], unavailable)
        };

        // Le profil doit pouvoir être relu et accepté par le testeur : on rejoue la validation sur le JSON produit.
        ControllerLocalProfile? parsed = JsonSerializer.Deserialize<ControllerLocalProfile>(ControllerProfileExportService.CreateJson(draft));
        if (parsed == null || !ControllerLocalProfileService.IsValid(parsed, controller.HardwareVendorId, controller.HardwareProductId,
                buttonCount, axisCount, switchCount, connection))
        {
            error = Tr("Le profil obtenu n’est pas valide pour ce matériel. Vérifiez les axes (types, extrémités, centre) et réessayez.");
            return null;
        }
        return draft;
    }

    private static JoystickAxisConfiguration? BuildAxis(InputRow row, out string? error)
    {
        error = null;
        if (!TryNumber(row.Min, out double min) || !TryNumber(row.Max, out double max) || !TryNumber(row.Center, out double center) ||
            !TryNumber(row.Dead, out double dead) || min < 0 || max > 1 || center < 0 || center > 1 || dead < 0 || dead > .5)
        {
            error = Tf("{0} : valeurs invalides. Min, centre et max doivent être entre 0 et 1, la zone morte entre 0 et 0,5.", row.Label);
            return null;
        }
        if (max - min <= .02)
        {
            error = Tf("{0} : amplitude trop faible. Bougez cet axe de bout en bout, ou réglez Min et Max à la main.", row.Label);
            return null;
        }
        string type = AxisTypeChoices[Math.Max(0, row.TypeBox!.SelectedIndex)].Value;
        center = row.NoCenter!.IsChecked == true ? min : Math.Clamp(center, min, max);
        double positive = Math.Abs(max - center) >= Math.Abs(min - center) ? max : min;
        double negative = positive == max ? min : max;
        if (type == "Stick" && (center - min <= .01 || max - center <= .01))
        {
            error = Tf("{0} : le centre d’un stick doit se trouver entre ses deux extrémités. Choisissez un autre type (gaz, curseur…) ou corrigez le centre.", row.Label);
            return null;
        }
        return new JoystickAxisConfiguration(row.Index, center, positive, negative)
        {
            Type = type,
            Inverted = row.Invert!.IsChecked == true,
            Name = row.Name,
            Deadzone = dead
        };
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONFIGURATION LIBRE (MATÉRIEL NON LISTÉ)
    // ============================================================================
}
