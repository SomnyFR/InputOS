using System;

namespace InputOS;

public partial class App : System.Windows.Application
{
    public InputOS.Services.ApplicationUpdateService? Updates { get; private set; }

    // ============================================================================
    // FONCTIONNALITÉ : INITIALISATION DE L'APPLICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    protected override void OnStartup(
        System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);


        InputOS.Services.LocalizationService.Apply(
            InputOS.Services.SettingsService.Load().Language);


        // Barre de titre sombre sur toutes les fenêtres (Windows 11 ; ignoré si non pris en charge).
        System.Windows.EventManager.RegisterClassHandler(
            typeof(System.Windows.Window),
            System.Windows.FrameworkElement.LoadedEvent,
            new System.Windows.RoutedEventHandler(
                (sender, _) =>
                {
                    ApplyDarkTitleBar((System.Windows.Window)sender);
                    InputOS.Services.LocalizationService.Translate((System.Windows.Window)sender);
                }));


        try
        {
            MainWindow mainWindow =
                new();


            MainWindow =
                mainWindow;


            mainWindow.Show();
            Updates = new InputOS.Services.ApplicationUpdateService();
            Updates.Start();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                exception.ToString(),
                InputOS.Services.LocalizationService.Tr("Erreur au démarrage d'InputOS"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);


            Shutdown();
        }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);

    private static void ApplyDarkTitleBar(
        System.Windows.Window window)
    {
        IntPtr handle =
            new System.Windows.Interop.WindowInteropHelper(window).Handle;


        int dark = 1;
        int caption = 0x00161210;   // #101216 (BGR)
        int text = 0x00F0EEED;      // #EDEEF0 (BGR)

        DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));     // mode sombre
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));  // couleur de la barre
        DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));     // couleur du titre
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Updates?.Stop();
        base.OnExit(e);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : INITIALISATION DE L'APPLICATION
    // ============================================================================

}
