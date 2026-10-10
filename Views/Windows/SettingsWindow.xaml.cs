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
        if (((App)System.Windows.Application.Current).Updates is { } updates)
        {
            updates.Changed += Updates_Changed;
            Closed += (_, _) => updates.Changed -= Updates_Changed;
        }
        RefreshUpdateStatus();


        CloseToTrayCheckBox.Checked +=
            CloseToTrayCheckBox_Changed;


        CloseToTrayCheckBox.Unchecked +=
            CloseToTrayCheckBox_Changed;
    }

    private void Updates_Changed(object? sender, EventArgs e) => RefreshUpdateStatus();
    private void RefreshUpdateStatus()
    {
        var updates = ((App)System.Windows.Application.Current).Updates;
        UpdateStatusText.Text = updates?.Status ?? "Vérification disponible après le démarrage.";
        CheckUpdatesButton.IsEnabled = updates != null && !updates.IsBusy;
        RestartForUpdateButton.Visibility = updates?.CanRestart == true ? Visibility.Visible : Visibility.Collapsed;
    }
    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        if (((App)System.Windows.Application.Current).Updates is { } updates) await updates.CheckAsync();
    }
    private void RestartForUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        try { ((App)System.Windows.Application.Current).Updates?.Restart(); }
        catch (Exception) { UpdateStatusText.Text = "Impossible de redémarrer pour le moment. Fermez puis relancez InputOS pour appliquer la mise à jour."; }
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
