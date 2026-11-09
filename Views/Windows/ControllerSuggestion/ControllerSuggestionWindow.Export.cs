using static InputOS.Services.LocalizationService;
using System.Windows;
using InputOS.Services;

namespace InputOS;

public partial class ControllerSuggestionWindow
{
    private readonly CancellationTokenSource diagnosticCancellation = new();
    private string? sentDiagnostic;
    // ============================================================================
    // FONCTIONNALITÉ : ENREGISTREMENT LOCAL ET PROPOSITION DE PROFIL
    // ============================================================================

    private void SaveLocalProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingConfiguration?.Buttons == null) return;
        try
        {
            string path = ControllerProfileExportService.SaveToProfilesDirectory(PendingConfiguration);
            ProfileExportFeedbackText.Text = Tf("Profil enregistré : {0}\nCe fichier sera conservé après la fermeture de cette fenêtre. Il reste à valider par les développeurs avant son intégration dans InputOS.", path);
        }
        catch (Exception)
        {
            ProfileExportFeedbackText.Text = Tr("Impossible d’enregistrer le profil. Vérifiez l’accès à Documents\\InputOS\\profils et réessayez.");
        }
    }

    private async void SendDiagnosticButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingConfiguration?.Buttons == null) return;
        var draft = PendingConfiguration;
        SendDiagnosticButton.IsEnabled = false;
        ProfileExportFeedbackText.Text = Tr("Envoi du diagnostic en cours…");
        try
        {
            string json = ControllerProfileExportService.CreateJson(draft, false);
            if (sentDiagnostic == json)
            {
                ProfileExportFeedbackText.Text = Tr("Ce diagnostic a déjà été envoyé aux développeurs.");
                return;
            }
            int issueNumber = await ControllerDiagnosticService.SendAsync(draft, diagnosticCancellation.Token);
            sentDiagnostic = json;
            if (!isClosed && ReferenceEquals(draft, PendingConfiguration))
                ProfileExportFeedbackText.Text = Tf("Diagnostic envoyé aux développeurs. Référence GitHub : #{0}. Merci pour votre contribution !", issueNumber);
        }
        catch (InvalidOperationException exception)
        {
            if (!isClosed) ProfileExportFeedbackText.Text = exception.Message;
        }
        catch (OperationCanceledException)
        {
            if (!isClosed) ProfileExportFeedbackText.Text = Tr("L’envoi n’a pas été confirmé dans le délai prévu. Réessayez plus tard : un diagnostic identique ne créera pas une nouvelle issue.");
        }
        catch (Exception)
        {
            if (!isClosed) ProfileExportFeedbackText.Text = Tr("Impossible d’envoyer le diagnostic. Vérifiez votre connexion Internet et réessayez. Vous pouvez enregistrer votre profil localement.");
        }
        finally
        {
            if (!isClosed) SendDiagnosticButton.IsEnabled = true;
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ENREGISTREMENT LOCAL ET PROPOSITION DE PROFIL
    // ============================================================================
}
