using System.IO;
using System.Text.Json;
using System.Net.Http;
using InputOS.Models;

namespace InputOS.Services;

public static class ControllerProfileExportService
{
    // ============================================================================

    public static string GetProfilesDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InputOS", "profils");

    public static string SaveToProfilesDirectory(ControllerConfigurationDraft draft)
    {
        string directory = GetProfilesDirectory();
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"InputOS-{draft.VendorId:X4}-{draft.ProductId:X4}.json");
        Save(path, draft);
        return path;
    }

    public static async Task<bool?> AreGitHubIssuesEnabledAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InputOS/1.0");
            using var response = await client.GetAsync("https://api.github.com/repos/SomnyFR/InputOS");
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.TryGetProperty("has_issues", out var value) &&
                value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
        }
        catch (Exception)
        {
            // Une indisponibilité de l’API ne doit pas empêcher l’ouverture du navigateur.
            return null;
        }
    }
    // FONCTIONNALITÉ : PROFIL LOCAL ET DIAGNOSTIC GITHUB
    // ============================================================================

    public static string CreateJson(ControllerConfigurationDraft draft, bool indented = true)
    {
        if (draft.Detection == null || draft.Joysticks == null || draft.Buttons == null)
            throw new InvalidOperationException("Terminez la configuration et enregistrez les associations des boutons avant de continuer.");
        // Exporter uniquement les informations utiles à la configuration,
        // sans identifiant de périphérique, chemin local ni nom de compte Windows.
        return JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            ProfileType = "UserConfiguration",
            ValidatedByInputOS = false,
            Model = draft.Name,
            VendorId = draft.VendorId,
            ProductId = draft.ProductId,
            Connection = "USB",
            ButtonCount = draft.Detection.ButtonStates.Length,
            AxisCount = draft.Detection.AxisValues.Length,
            SwitchCount = draft.Detection.SwitchPositions.Length,
            draft.Joysticks,
            draft.Buttons
        }, new JsonSerializerOptions { WriteIndented = indented });
    }

    public static void Save(string path, ControllerConfigurationDraft draft)
    {
        string json = CreateJson(draft);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static string CreateIssueBody(ControllerConfigurationDraft draft) =>
        $"### Proposition de profil InputOS\n\n" +
        $"Modèle : {draft.Name}\n\nVID : {draft.VendorId:X4} · PID : {draft.ProductId:X4} · USB\n\n" +
        "Configuration réalisée par l’utilisateur, à vérifier avant intégration dans InputOS.\n\n" +
        "### Configuration et diagnostic\n\n```json\n" + CreateJson(draft, false) + "\n```\n\n" +
        "### Informations complémentaires\n\nAjoutez ici les éventuels problèmes rencontrés.";

    public static string CreateIssueUrl(ControllerConfigurationDraft draft, bool includeBody = true)
    {
        string title = $"[Manette] {draft.Name} ({draft.VendorId:X4}:{draft.ProductId:X4})";
        return "https://github.com/SomnyFR/InputOS/issues/new?title=" + Uri.EscapeDataString(title) +
            (includeBody ? "&body=" + Uri.EscapeDataString(CreateIssueBody(draft)) : "");
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : PROFIL LOCAL ET DIAGNOSTIC GITHUB
    // ============================================================================
}
