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
            new("Ouvrir InputOS");


        System.Windows.Forms.ToolStripMenuItem quitItem =
            new("Quitter InputOS");


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
                    "0 manette",

                1 =>
                    "1 manette",

                _ =>
                    $"{detectedControllerCount} manettes"
            };


        string tooltip =
            $"InputOS - {controllerText}";


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

    private const ushort SonyVendorId =
        0x054C;


    private const ushort MicrosoftVendorId =
        0x045E;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    private void RefreshControllerList()
    {
        DeviceTreeView.Items.Clear();


        selectedControllerName =
            null;


        TreeViewItem controllersRoot =
            new()
            {
                Header =
                    "Manettes",

                IsExpanded =
                    true
            };


        detectedControllerCount =
            0;


        foreach (
            RawGameController controller
            in RawGameController.RawGameControllers)
        {
            if (!IsSupportedOfficialController(
                    controller))
            {
                continue;
            }


            TreeViewItem controllerItem =
                new()
                {
                    Header =
                        GetControllerDisplayName(
                            controller),

                    Tag =
                        controller
                };


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
                        "Aucune manette détectée",

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
                    "Aucune manette détectée",

                1 =>
                    "1 manette détectée",

                _ =>
                    $"{detectedControllerCount} manettes détectées"
            };


        UpdateTrayTooltip();
    }


    private static bool IsSupportedOfficialController(
        RawGameController controller)
    {
        ushort vendorId =
            controller.HardwareVendorId;


        return vendorId == SonyVendorId ||
               vendorId == MicrosoftVendorId;
    }


    private static string GetControllerDisplayName(
        RawGameController controller)
    {
        ushort vendorId =
            controller.HardwareVendorId;


        ushort productId =
            controller.HardwareProductId;


        if (vendorId == SonyVendorId)
        {
            return productId switch
            {
                0x05C4 =>
                    "DualShock 4",

                0x09CC =>
                    "DualShock 4",

                0x0CE6 =>
                    "DualSense",

                0x0DF2 =>
                    "DualSense Edge",

                _ =>
                    string.IsNullOrWhiteSpace(
                        controller.DisplayName)
                        ? "Manette PlayStation"
                        : controller.DisplayName
            };
        }


        if (vendorId == MicrosoftVendorId)
        {
            if (!string.IsNullOrWhiteSpace(
                    controller.DisplayName))
            {
                return controller.DisplayName;
            }


            return "Manette Xbox";
        }


        return string.IsNullOrWhiteSpace(
            controller.DisplayName)
            ? "Manette"
            : controller.DisplayName;
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

    private void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StatusText.Text =
            "Actualisation des périphériques...";


        ClearControllerInformation();


        RefreshControllerList();
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
            GetControllerDisplayName(
                controller);


        await DisplayControllerInformationAsync(
            controller);


        UpdateTrayTooltip();
    }


    private async Task DisplayControllerInformationAsync(
        RawGameController controller)
    {
        DeviceNameText.Text =
            GetControllerDisplayName(
                controller);


        DeviceTypeText.Text =
            GetControllerType(
                controller);


        DeviceConnectionText.Text =
            controller.IsWireless
                ? "Sans fil"
                : "Filaire";


        DeviceVidText.Text =
            $"0x{controller.HardwareVendorId:X4}";


        DevicePidText.Text =
            $"0x{controller.HardwareProductId:X4}";


        DeviceButtonsText.Text =
            controller.ButtonCount.ToString();


        DeviceAxesText.Text =
            controller.AxisCount.ToString();


        DeviceFirmwareText.Text =
            "Non disponible";


        DeviceBatteryText.Text =
            "Lecture...";


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
                "Non disponible";
        }


        DeviceStatusText.Text =
            "Connectée";


        StatusText.Text =
            $"{GetControllerDisplayName(controller)} sélectionnée";
    }


    private static string GetControllerType(
        RawGameController controller)
    {
        return controller.HardwareVendorId switch
        {
            SonyVendorId =>
                "PlayStation",

            MicrosoftVendorId =>
                "Xbox",

            _ =>
                "Inconnu"
        };
    }


    private void ClearControllerInformation()
    {
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
            "Non disponible";


        DeviceBatteryText.Text =
            "Non disponible";


        DeviceStatusText.Text =
            "Aucun périphérique sélectionné";
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : SÉLECTION D'UNE MANETTE
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
                "Sélectionnez d'abord une manette.",
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