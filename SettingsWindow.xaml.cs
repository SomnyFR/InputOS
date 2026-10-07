using System.Windows;
using InputOS.Models;
using InputOS.Services;

namespace InputOS;

public partial class SettingsWindow : Window
{

    // ============================================================================
    // FONCTIONNALITÉ : PARAMÈTRES DE L'APPLICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private readonly AppSettings settings;


    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public SettingsWindow()
    {
        InitializeComponent();


        settings =
            SettingsService.Load();


        LoadSettingsIntoInterface();


        CloseToTrayCheckBox.Checked +=
            CloseToTrayCheckBox_Changed;


        CloseToTrayCheckBox.Unchecked +=
            CloseToTrayCheckBox_Changed;
    }


    private void LoadSettingsIntoInterface()
    {
        CloseToTrayCheckBox.IsChecked =
            settings.CloseToTray;
    }


    private void CloseToTrayCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        settings.CloseToTray =
            CloseToTrayCheckBox.IsChecked == true;


        SettingsService.Save(
            settings);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : PARAMÈTRES DE L'APPLICATION
    // ============================================================================

}