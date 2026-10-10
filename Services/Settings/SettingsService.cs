using System;
using System.IO;
using System.Text.Json;
using InputOS.Models;

namespace InputOS.Services;

public static class SettingsService
{

    // ============================================================================
    // FONCTIONNALITÉ : CONFIGURATION DU STOCKAGE
    // ============================================================================

    // ---------------------------------------------------------------------------
    // CONFIGURATION
    // ---------------------------------------------------------------------------

    private static readonly string SettingsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "InputOS");

    private static readonly string SettingsFilePath =
        Path.Combine(
            SettingsDirectory,
            "settings.json");

    // ============================================================================
    // FIN FONCTIONNALITÉ : CONFIGURATION DU STOCKAGE
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : CHARGEMENT DES PARAMÈTRES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }


            string json =
                File.ReadAllText(
                    SettingsFilePath);


            AppSettings? settings =
                JsonSerializer.Deserialize<AppSettings>(
                    json);


            return settings ??
                   new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : CHARGEMENT DES PARAMÈTRES
    // ============================================================================



    // ============================================================================
    // FONCTIONNALITÉ : ENREGISTREMENT DES PARAMÈTRES
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public static void Save(
        AppSettings settings)
    {
        Directory.CreateDirectory(
            SettingsDirectory);


        JsonSerializerOptions options =
            new()
            {
                WriteIndented = true
            };


        string json =
            JsonSerializer.Serialize(
                settings,
                options);


        File.WriteAllText(
            SettingsFilePath,
            json);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ENREGISTREMENT DES PARAMÈTRES
    // ============================================================================

}