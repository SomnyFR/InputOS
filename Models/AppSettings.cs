namespace InputOS.Models;

public class AppSettings
{

    // ============================================================================
    // FONCTIONNALITÉ : PARAMÈTRES DE L'APPLICATION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    public bool CloseToTray { get; set; } = true;


    // Langue de l'interface (code : fr, en…). Lue uniquement au démarrage.
    public string Language { get; set; } = "fr";


    // Cache : « Ne plus afficher ce message » avant suppression, une valeur par ligne.
    public bool SkipDeleteProfilesWarning { get; set; }

    public bool SkipDeleteSettingsWarning { get; set; }


    // ============================================================================
    // FIN FONCTIONNALITÉ : PARAMÈTRES DE L'APPLICATION
    // ============================================================================

}