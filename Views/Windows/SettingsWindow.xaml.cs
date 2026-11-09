using static InputOS.Services.LocalizationService;
using System.IO;
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


        LanguageComboBox.SelectionChanged +=
            LanguageComboBox_SelectionChanged;


        CloseToTrayCheckBox.Checked +=
            CloseToTrayCheckBox_Changed;


        CloseToTrayCheckBox.Unchecked +=
            CloseToTrayCheckBox_Changed;
    }

    private void Updates_Changed(object? sender, EventArgs e) => RefreshUpdateStatus();
    private void RefreshUpdateStatus()
    {
        var updates = ((App)System.Windows.Application.Current).Updates;
        UpdateStatusText.Text = updates?.Status ?? Tr("Vérification disponible après le démarrage.");
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
        catch (Exception) { UpdateStatusText.Text = Tr("Impossible de redémarrer pour le moment. Fermez puis relancez InputOS pour appliquer la mise à jour."); }
    }


    private static string FormatSize(long bytes) =>
        bytes < 1024 ? Tf(bytes > 1 ? "{0} octets" : "{0} octet", bytes)
        : bytes < 1024 * 1024 ? Tf("{0:0.#} Ko", bytes / 1024.0)
        : Tf("{0:0.#} Mo", bytes / (1024.0 * 1024.0));

    private void RefreshCacheSizes()
    {
        long profiles = 0;
        try
        {
            string directory = ControllerProfileExportService.GetProfilesDirectory();
            if (Directory.Exists(directory))
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    profiles += new FileInfo(file).Length;
        }
        catch (Exception) { }

        ProfilesSizeText.Text = FormatSize(profiles);
        LogsSizeText.Text = FormatSize(0);
        SettingsSizeText.Text = FormatSize(SettingsService.GetSize());
    }

    private void LoadSettingsIntoInterface()
    {
        LanguageComboBox.ItemsSource = LocalizationService.Available;
        LanguageComboBox.SelectedValue = settings.Language;
        RefreshCacheSizes();
        CloseToTrayCheckBox.IsChecked =
            settings.CloseToTray;
    }


    // La langue est enregistrée mais appliquée seulement au prochain démarrage.
    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedValue is not string code || code == settings.Language)
            return;

        settings.Language = code;
        SettingsService.Save(settings);

        if (MessageBox.Show(this,
                LocalizationService.Get("Settings.Language.RestartMessage"),
                LocalizationService.Get("Settings.Language.RestartTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            System.Diagnostics.Process.Start(Environment.ProcessPath!);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "InputOS", MessageBoxButton.OK, MessageBoxImage.Warning); }
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

    // ---------------------------------------------------------------------------
    // CACHE : SUPPRESSIONS (toujours précédées de l'avertissement, sauf case cochée)
    // ---------------------------------------------------------------------------

    private void DeleteProfilesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm(settings.SkipDeleteProfilesWarning, Tr(Tr("tous les profils enregistrés (y compris archive)")), v => settings.SkipDeleteProfilesWarning = v))
            return;

        string directory = ControllerProfileExportService.GetProfilesDirectory();
        try
        {
            if (Directory.Exists(directory))
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    File.Delete(file);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "InputOS", MessageBoxButton.OK, MessageBoxImage.Warning); }

        RefreshCacheSizes();
    }

    private void DeleteSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm(settings.SkipDeleteSettingsWarning, Tr(Tr("les paramètres (settings.json)")), v => settings.SkipDeleteSettingsWarning = v))
            return;

        // Les réglages reviennent aux valeurs par défaut, y compris les cases « Ne plus afficher ».
        try { SettingsService.Delete(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "InputOS", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        settings.CloseToTray = new AppSettings().CloseToTray;
        settings.Language = new AppSettings().Language;
        settings.SkipDeleteProfilesWarning = false;
        settings.SkipDeleteSettingsWarning = false;
        LoadSettingsIntoInterface();
    }

    // Retourne true si la suppression doit avoir lieu ; mémorise la case dans les paramètres.
    private bool Confirm(bool skip, string what, Action<bool> remember)
    {
        if (skip) return true;

        DeleteWarningWindow dialog = new(what) { Owner = this };
        if (dialog.ShowDialog() != true) return false;

        if (dialog.DoNotShowAgain)
        {
            remember(true);
            SettingsService.Save(settings);
            RefreshCacheSizes();
        }
        return true;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : PARAMÈTRES DE L'APPLICATION
    // ============================================================================

}
