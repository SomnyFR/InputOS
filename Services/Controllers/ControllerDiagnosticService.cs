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

    public static async Task<int> SendAsync(ControllerConfigurationDraft draft, CancellationToken cancellation)
    {
        string settingsPath = Path.Combine(AppContext.BaseDirectory, "diagnostic-settings.json");
        using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath, cancellation));
        string? address = settings.RootElement.GetProperty("Endpoint").GetString();
        if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo))
            throw new InvalidOperationException("Le service d’envoi des diagnostics n’est pas encore configuré. Votre profil peut être enregistré localement en attendant.");

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        using var content = new StringContent(ControllerProfileExportService.CreateJson(draft, false), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content, cancellation);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("Trop de diagnostics ont été envoyés récemment. Réessayez un peu plus tard.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Le service n’a pas pu recevoir le diagnostic. Votre profil est conservé ; vous pourrez réessayer plus tard.");
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        if (!result.RootElement.TryGetProperty("issueNumber", out var number) || !number.TryGetInt32(out int issueNumber) || issueNumber <= 0)
            throw new InvalidOperationException("Le service n’a pas confirmé la réception du diagnostic. Réessayez plus tard.");
        return issueNumber;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ENVOI DIRECT DU DIAGNOSTIC AU SERVICE INPUTOS
    // ============================================================================
}
