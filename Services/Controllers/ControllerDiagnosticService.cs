using static InputOS.Services.LocalizationService;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using InputOS.Models;

namespace InputOS.Services;

public static class ControllerDiagnosticService
{
    // ============================================================================
    // FONCTIONNALITÉ : ENVOI DIRECT DU DIAGNOSTIC AU SERVICE INPUTOS
    // ============================================================================

    // Texte traduit selon le motif renvoyé par le Worker (jamais le texte brut du serveur).
    private static string ReasonMessage(string? reason) => reason switch
    {
        "joystick_range" or "joystick_value" or "joystick_axis_index" =>
            Tr("Le service a refusé une calibration d’axe (plage trop faible ou valeur hors limites). Refaites la calibration en bougeant l’axe sur toute sa course."),
        "axis_reused" => Tr("Le service a refusé le profil : le même axe est utilisé deux fois."),
        "text_invalid" => Tr("Le service a refusé un nom (vide, trop long ou avec un caractère interdit). Modifiez le nom de la manette ou des entrées."),
        "schema_unsupported" or "connection" or "controller_type" or "axis_type" or "joysticks_invalid" or "stick_pair" =>
            Tr("Le service n’accepte pas encore ce format de profil. Enregistrez-le localement et réessayez après une mise à jour."),
        "too_large" => Tr("Le profil est trop volumineux pour être envoyé."),
        _ => Tr("Le service a refusé le profil. Enregistrez-le localement ; vous pourrez réessayer plus tard.")
    };

    public static async Task<int> SendAsync(ControllerConfigurationDraft draft, CancellationToken cancellation)
    {
        try
        {
            int number = await SendCoreAsync(draft, cancellation);
            DiagnosticLogService.Append(draft.VendorId, draft.ProductId, $"OK issue #{number}");
            return number;
        }
        catch (DiagnosticSendException e)
        {
            DiagnosticLogService.Append(draft.VendorId, draft.ProductId, "ERREUR " + e.Code);
            throw;
        }
        catch (OperationCanceledException)
        {
            DiagnosticLogService.Append(draft.VendorId, draft.ProductId, "ERREUR délai ou annulation");
            throw;
        }
        catch (Exception)
        {
            DiagnosticLogService.Append(draft.VendorId, draft.ProductId, "ERREUR réseau ou configuration");
            throw;
        }
    }

    private static async Task<int> SendCoreAsync(ControllerConfigurationDraft draft, CancellationToken cancellation)
    {
        string settingsPath = Path.Combine(AppContext.BaseDirectory, "diagnostic-settings.json");
        using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath, cancellation));
        string? address = settings.RootElement.GetProperty("Endpoint").GetString();
        if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo))
            throw new DiagnosticSendException("service non configuré", Tr("Le service d’envoi des diagnostics n’est pas encore configuré. Votre profil peut être enregistré localement en attendant."));

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        using var content = new StringContent(ControllerProfileExportService.CreateJson(draft, false), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content, cancellation);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new DiagnosticSendException("429 trop d’envois", Tr("Trop de diagnostics ont été envoyés récemment. Réessayez un peu plus tard."));
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string? reason = null;
            try
            {
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
                reason = error.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
            }
            catch (JsonException) { }
            // Le motif n'est journalisé que s'il a la forme attendue (mot court), jamais du texte libre du serveur.
            if (reason != null && !System.Text.RegularExpressions.Regex.IsMatch(reason, "^[a-z_]{1,40}$")) reason = null;
            throw new DiagnosticSendException("400 invalid_diagnostic " + (reason ?? "sans motif"), ReasonMessage(reason));
        }
        if (!response.IsSuccessStatusCode)
            throw new DiagnosticSendException($"{(int)response.StatusCode} refus du service",
                Tr("Le service n’a pas pu recevoir le diagnostic. Votre profil est conservé ; vous pourrez réessayer plus tard."));
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        if (!result.RootElement.TryGetProperty("issueNumber", out var number) || !number.TryGetInt32(out int issueNumber) || issueNumber <= 0)
            throw new DiagnosticSendException("réponse sans numéro d’issue", Tr("Le service n’a pas confirmé la réception du diagnostic. Réessayez plus tard."));
        return issueNumber;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ENVOI DIRECT DU DIAGNOSTIC AU SERVICE INPUTOS
    // ============================================================================
}

// Message déjà traduit pour l'utilisateur ; Code ne sert qu'au journal local.
public sealed class DiagnosticSendException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
