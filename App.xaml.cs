using System;

namespace InputOS;

public partial class App : System.Windows.Application
{

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

    // ============================================================================
    // FIN FONCTIONNALITÉ : INITIALISATION DE L'APPLICATION
    // ============================================================================

}