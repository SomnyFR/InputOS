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
                "Erreur au démarrage d'InputOS",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);


            Shutdown();
        }
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
