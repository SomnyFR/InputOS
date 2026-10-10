namespace InputOS;

public static class Program
{
    // ============================================================================
    // FONCTIONNALITÉ : DÉMARRAGE ET APPLICATION DES MISES À JOUR
    // ============================================================================
    [STAThread]
    public static void Main(string[] args)
    {
        Velopack.VelopackApp.Build().Run();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
    // ============================================================================
    // FIN FONCTIONNALITÉ : DÉMARRAGE ET APPLICATION DES MISES À JOUR
    // ============================================================================
}
