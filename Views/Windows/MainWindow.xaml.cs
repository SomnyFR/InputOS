using static InputOS.Services.LocalizationService;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using InputOS.Models;
using InputOS.Services;
using Windows.Gaming.Input;

namespace InputOS;

public partial class MainWindow : Window
{

    // Illustration de la carte d'une manette ; null = icône générique.
    public static readonly DependencyProperty CardImageProperty =
        DependencyProperty.RegisterAttached(
            "CardImage",
            typeof(System.Windows.Media.ImageSource),
            typeof(MainWindow),
            new PropertyMetadata(null));

    public static void SetCardImage(DependencyObject element, System.Windows.Media.ImageSource? value) =>
        element.SetValue(CardImageProperty, value);

    public static System.Windows.Media.ImageSource? GetCardImage(DependencyObject element) =>
        (System.Windows.Media.ImageSource?)element.GetValue(CardImageProperty);

    private static readonly Dictionary<string, System.Windows.Media.ImageSource> controllerImages = new();

    // Association unique modèle -> illustration, partagée par la liste et le panneau d'informations.
    private static System.Windows.Media.ImageSource? GetControllerImage(
        RawGameController controller)
    {
        string? file =
            controller.HardwareVendorId switch
            {
                0x054C when controller.HardwareProductId is 0x05C4 or 0x09CC => "ps4-controller.png",
                0x054C when controller.HardwareProductId == 0x0CE6 => "dualsense.png",
                0x045E => "xbox-controller.png",
                _ => null
            };

        if (file == null) return null;

        if (!controllerImages.TryGetValue(file, out var image))
        {
            System.Windows.Media.Imaging.BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri($"pack://application:,,,/InputOS;component/Assets/Images/{file}");
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            controllerImages[file] = image = bitmap;
        }

        return image;
    }


    // ============================================================================
    // FONCTIONNALITÉ : INITIALISATION DE L'APPLICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private bool isExiting;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public MainWindow()
    {
        InitializeComponent();


        InitializeTrayIcon();


        RawGameController.RawGameControllerAdded +=
            RawGameController_Added;


        RawGameController.RawGameControllerRemoved +=
            RawGameController_Removed;


        Closing +=
            MainWindow_Closing;


        Closed +=
            MainWindow_Closed;


        RefreshControllerList();
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : INITIALISATION DE L'APPLICATION
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : ZONE DE NOTIFICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private System.Windows.Forms.NotifyIcon? trayIcon;

    private System.Windows.Forms.ContextMenuStrip? trayMenu;

    private string? selectedControllerName;

    private int detectedControllerCount;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void InitializeTrayIcon()
    {
        trayMenu =
            new System.Windows.Forms.ContextMenuStrip();


        System.Windows.Forms.ToolStripMenuItem openItem =
            new(Tr("Ouvrir InputOS"));


        System.Windows.Forms.ToolStripMenuItem quitItem =
            new(Tr("Quitter InputOS"));


        openItem.Click +=
            (_, _) =>
            {
                RestoreFromTray();
            };


        quitItem.Click +=
            (_, _) =>
            {
                ExitApplication();
            };


        trayMenu.Items.Add(
            openItem);


        trayMenu.Items.Add(
            quitItem);


        trayIcon =
            new System.Windows.Forms.NotifyIcon
            {
                Visible = false,

                ContextMenuStrip =
                    trayMenu,

                Text =
                    "InputOS"
            };


        LoadTrayIcon();


        trayIcon.DoubleClick +=
            NotifyIcon_DoubleClick;


        UpdateTrayTooltip();
    }


    private void LoadTrayIcon()
    {
        if (trayIcon == null)
        {
            return;
        }


        try
        {
            Uri iconUri =
                new(
                    "pack://application:,,,/Assets/Icons/iOSBlack.ico",
                    UriKind.Absolute);


            System.Windows.Resources.StreamResourceInfo? resource =
                System.Windows.Application.GetResourceStream(
                    iconUri);


            if (resource == null)
            {
                return;
            }


            using System.IO.Stream stream =
                resource.Stream;


            using Icon sourceIcon =
                new(stream);


            trayIcon.Icon =
                (Icon)sourceIcon.Clone();
        }
        catch
        {
            // L'absence de l'icône ne doit pas empêcher
            // InputOS de fonctionner.
        }
    }


    private void NotifyIcon_DoubleClick(
        object? sender,
        EventArgs e)
    {
        RestoreFromTray();
    }


    private void RestoreFromTray()
    {
        Show();


        if (WindowState == WindowState.Minimized)
        {
            WindowState =
                WindowState.Normal;
        }


        ShowInTaskbar =
            true;


        Activate();


        if (trayIcon != null)
        {
            trayIcon.Visible =
                false;
        }
    }


    private void SendToTray()
    {
        Hide();


        ShowInTaskbar =
            false;


        UpdateTrayTooltip();


        if (trayIcon != null)
        {
            trayIcon.Visible =
                true;
        }
    }


    private void ExitApplication()
    {
        isExiting =
            true;


        if (trayIcon != null)
        {
            trayIcon.Visible =
                false;
        }


        Close();


        System.Windows.Application.Current.Shutdown();
    }


    private void UpdateTrayTooltip()
    {
        if (trayIcon == null)
        {
            return;
        }


        string controllerText =
            detectedControllerCount switch
            {
                0 =>
                    Tr("0 manette"),

                1 =>
                    Tr("1 manette"),

                _ =>
                    Tf("{0} manettes", detectedControllerCount)
            };


        string tooltip =
            Tf("InputOS - {0}", controllerText);


        if (!string.IsNullOrWhiteSpace(
                selectedControllerName))
        {
            tooltip +=
                $" - {selectedControllerName}";
        }


        // NotifyIcon limite la longueur de son texte.
        if (tooltip.Length > 63)
        {
            tooltip =
                tooltip[..63];
        }


        trayIcon.Text =
            tooltip;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ZONE DE NOTIFICATION
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : DÉTECTION DES MANETTES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    // La compatibilité est centralisée dans ControllerCompatibilityService.


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void RefreshControllerList()
    {
        ClearControllerInformation();


        DeviceTreeView.Items.Clear();


        selectedControllerName =
            null;


        TreeViewItem controllersRoot =
            new()
            {
                Header =
                    Tr("Manettes"),

                IsExpanded =
                    true
            };


        detectedControllerCount =
            0;


        foreach (
            RawGameController controller
            in RawGameController.RawGameControllers)
        {
            TreeViewItem controllerItem =
                new()
                {
                    Header =
                        ControllerCompatibilityService.GetControllerDisplayName(
                            controller),

                    Tag =
                        controller
                };


            SetCardImage(
                controllerItem,
                GetControllerImage(controller));


            controllersRoot.Items.Add(
                controllerItem);


            detectedControllerCount++;
        }


        if (detectedControllerCount == 0)
        {
            TreeViewItem emptyItem =
                new()
                {
                    Header =
                        Tr("Aucune manette détectée"),

                    IsEnabled =
                        false
                };


            controllersRoot.Items.Add(
                emptyItem);
        }


        DeviceTreeView.Items.Add(
            controllersRoot);


        StatusText.Text =
            detectedControllerCount switch
            {
                0 =>
                    Tr("Aucune manette détectée"),

                1 =>
                    Tr("1 manette détectée"),

                _ =>
                    Tf("{0} manettes détectées", detectedControllerCount)
            };


        UpdateTrayTooltip();
    }


    // ============================================================================
    // FIN FONCTIONNALITÉ : DÉTECTION DES MANETTES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : CONNEXION ET DÉCONNEXION À CHAUD
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void RawGameController_Added(
        object? sender,
        RawGameController controller)
    {
        Dispatcher.Invoke(
            RefreshControllerList);
    }


    private void RawGameController_Removed(
        object? sender,
        RawGameController controller)
    {
        Dispatcher.Invoke(
            () =>
            {
                ClearControllerInformation();

                selectedControllerName =
                    null;


                RefreshControllerList();
            });
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONNEXION ET DÉCONNEXION À CHAUD
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : ACTUALISATION MANUELLE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (RefreshProgressBar.Visibility == Visibility.Visible)
            return;


        StatusText.Text =
            Tr("Actualisation des périphériques...");


        RefreshProgressBar.Visibility =
            Visibility.Visible;


        // Laisse WPF afficher la barre avant la lecture (synchrone) des manettes.
        await System.Windows.Threading.Dispatcher.Yield(
            System.Windows.Threading.DispatcherPriority.Render);


        try
        {
            ClearControllerInformation();


            RefreshControllerList();
        }
        finally
        {
            RefreshProgressBar.Visibility =
                Visibility.Collapsed;
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ACTUALISATION MANUELLE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : SÉLECTION D'UNE MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private async void DeviceTreeView_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem item)
        {
            selectedControllerName =
                null;


            ClearControllerInformation();


            UpdateTrayTooltip();


            return;
        }


        if (item.Tag is not RawGameController controller)
        {
            selectedControllerName =
                null;


            ClearControllerInformation();


            UpdateTrayTooltip();


            return;
        }


        selectedControllerName =
            ControllerCompatibilityService.GetControllerDisplayName(
                controller);


        await DisplayControllerInformationAsync(
            controller);


        UpdateTrayTooltip();
    }


    private async Task DisplayControllerInformationAsync(
        RawGameController controller)
    {
        var localProfile = ControllerLocalProfileService.Load(controller);
        TesterButton.IsEnabled =
            ControllerCompatibilityService.HasValidatedProfile(controller) || localProfile != null;
        ControllerCompatibilityWarningText.Text = localProfile != null
            ? Tr("Profil local provisoire actif. Un profil officiel prendra la priorité lorsqu’il sera disponible.")
            : Tr("Manette non compatible. Aidez-nous à l’ajouter à InputOS.");


        ControllerCompatibilityWarning.Visibility =
            ControllerCompatibilityService.HasValidatedProfile(controller)
                ? Visibility.Collapsed
                : Visibility.Visible;


        DeviceNameText.Text =
            ControllerCompatibilityService.GetControllerDisplayName(
                controller);


        UpdateDeviceImage(
            controller);


        DeviceTypeText.Text =
            ControllerCompatibilityService.GetControllerType(
                controller);


        DeviceConnectionText.Text =
            controller.IsWireless
                ? Tr("Sans fil")
                : Tr("Filaire");


        DeviceVidText.Text =
            $"0x{controller.HardwareVendorId:X4}";


        DevicePidText.Text =
            $"0x{controller.HardwareProductId:X4}";


        DeviceButtonsText.Text =
            controller.ButtonCount.ToString();


        DeviceAxesText.Text =
            controller.AxisCount.ToString();


        DeviceFirmwareText.Text =
            Tr("Non disponible");


        DeviceBatteryText.Text =
            Tr("Lecture...");


        DeviceStatusText.Text =
            Tr("Connectée");


        StatusText.Text =
            Tf("{0} sélectionnée", ControllerCompatibilityService.GetControllerDisplayName(controller));


        string? batteryDisplay =
            await ControllerBatteryService.GetBatteryDisplayAsync(
                controller);


        if (DeviceTreeView.SelectedItem
                is TreeViewItem selectedItem &&
            ReferenceEquals(
                selectedItem.Tag,
                controller))
        {
            DeviceBatteryText.Text =
                batteryDisplay ??
                Tr("Non disponible");
        }



    }


    private void UpdateDeviceImage(
        RawGameController controller)
    {
        DeviceImage.Source =
            GetControllerImage(
                controller);


        DeviceImage.Visibility =
            DeviceImage.Source == null
                ? Visibility.Collapsed
                : Visibility.Visible;
    }


    private void ClearControllerInformation()
    {
        DeviceImage.Source =
            null;


        DeviceImage.Visibility =
            Visibility.Collapsed;


        TesterButton.IsEnabled =
            false;


        ControllerCompatibilityWarning.Visibility =
            Visibility.Collapsed;


        DeviceNameText.Text =
            "-";


        DeviceTypeText.Text =
            "-";


        DeviceConnectionText.Text =
            "-";


        DeviceVidText.Text =
            "-";


        DevicePidText.Text =
            "-";


        DeviceButtonsText.Text =
            "-";


        DeviceAxesText.Text =
            "-";


        DeviceFirmwareText.Text =
            Tr("Non disponible");


        DeviceBatteryText.Text =
            Tr("Non disponible");


        DeviceStatusText.Text =
            Tr("Aucun périphérique sélectionné");
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : SÉLECTION D'UNE MANETTE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : SUGGESTION D'UNE MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void SuggestControllerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DeviceTreeView.SelectedItem
                is not TreeViewItem selectedItem ||
            selectedItem.Tag
                is not RawGameController controller ||
            ControllerCompatibilityService.HasValidatedProfile(controller))
        {
            return;
        }


        if (controller.IsWireless ||
            !RawGameController.RawGameControllers.Contains(controller))
        {
            MessageBox.Show(
                this,
                Tr("Pour suggérer cette manette, branchez-la avec un câble USB, puis sélectionnez sa connexion filaire dans InputOS."),
                Tr("Connexion filaire requise"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);


            return;
        }


        ControllerSuggestionWindow suggestionWindow =
            new(controller)
            {
                Owner =
                    this
            };


        suggestionWindow.ShowDialog();
        _ = DisplayControllerInformationAsync(controller);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : SUGGESTION D'UNE MANETTE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : OUVERTURE DU TESTEUR DE MANETTE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void TesterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DeviceTreeView.SelectedItem
                is not TreeViewItem selectedItem ||
            selectedItem.Tag
                is not RawGameController controller)
        {
            MessageBox.Show(
                Tr("Sélectionnez d'abord une manette."),
                "InputOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);


            return;
        }


        TesterWindow testerWindow =
            new(controller)
            {
                Owner =
                    this
            };


        testerWindow.Show();
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : OUVERTURE DU TESTEUR DE MANETTE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : OUVERTURE DES PARAMÈTRES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SettingsWindow settingsWindow =
            new()
            {
                Owner =
                    this
            };


        settingsWindow.ShowDialog();
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : OUVERTURE DES PARAMÈTRES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : FERMER VERS LA ZONE DE NOTIFICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void MainWindow_Closing(
        object? sender,
        CancelEventArgs e)
    {
        if (isExiting)
        {
            return;
        }


        AppSettings settings =
            SettingsService.Load();


        if (!settings.CloseToTray)
        {
            return;
        }


        e.Cancel =
            true;


        SendToTray();
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : FERMER VERS LA ZONE DE NOTIFICATION
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : FERMETURE DE L'APPLICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void MainWindow_Closed(
        object? sender,
        EventArgs e)
    {
        RawGameController.RawGameControllerAdded -=
            RawGameController_Added;


        RawGameController.RawGameControllerRemoved -=
            RawGameController_Removed;


        if (trayIcon != null)
        {
            trayIcon.Visible =
                false;


            trayIcon.Dispose();


            trayIcon =
                null;
        }


        if (trayMenu != null)
        {
            trayMenu.Dispose();


            trayMenu =
                null;
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : FERMETURE DE L'APPLICATION
    // ============================================================================

}
