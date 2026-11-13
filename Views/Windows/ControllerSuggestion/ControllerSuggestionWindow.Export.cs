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
        if (PendingConfiguration?.Buttons == null)
        {
            ProfileExportFeedbackText.Text = Tr("Enregistrement impossible : terminez la configuration avant d’enregistrer le profil.");
            ProfileExportFeedbackText.BringIntoView();
            return;
        }
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

    // Le résultat est écrit dans le texte de retour (rendu visible par défilement) et dans une boîte de dialogue.
    private void ShowDiagnosticResult(string message, bool success)
    {
        if (isClosed) return;
        ProfileExportFeedbackText.Text = message;
        ProfileExportFeedbackText.BringIntoView();
        MessageBox.Show(this, message, "InputOS", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private async void SendDiagnosticButton_Click(object sender, RoutedEventArgs e)
    {
        var draft = PendingConfiguration;
        if (draft?.Buttons == null || draft.Detection == null || draft.Joysticks == null)
        {
            ShowDiagnosticResult(Tr("Envoi impossible : terminez la configuration (axes et boutons) avant d’envoyer le diagnostic."), false);
            return;
        }
        string json;
        try { json = ControllerProfileExportService.CreateJson(draft, false); }
        catch (Exception)
        {
            ShowDiagnosticResult(Tr("Envoi impossible : le profil n’a pas pu être préparé. Refaites la configuration."), false);
            return;
        }
        if (sentDiagnostic == json)
        {
            ShowDiagnosticResult(Tr("Ce diagnostic a déjà été envoyé aux développeurs."), true);
            return;
        }
        // Même contenu que l'envoi, seulement indenté pour la lecture.
        var preview = new DiagnosticPreviewWindow(ControllerProfileExportService.CreateJson(draft)) { Owner = this };
        if (preview.ShowDialog() != true) return;

        SendDiagnosticButton.IsEnabled = false;
        ProfileExportFeedbackText.Text = Tr("Envoi du diagnostic en cours…");
        ProfileExportFeedbackText.BringIntoView();
        try
        {
            int issueNumber = await ControllerDiagnosticService.SendAsync(draft, diagnosticCancellation.Token);
            sentDiagnostic = json;
            ShowDiagnosticResult(Tf("Diagnostic envoyé aux développeurs. Référence GitHub : #{0}. Merci pour votre contribution !", issueNumber), true);
        }
        catch (InvalidOperationException exception)
        {
            ShowDiagnosticResult(exception.Message, false);
        }
        catch (OperationCanceledException)
        {
            ShowDiagnosticResult(Tr("L’envoi n’a pas été confirmé dans le délai prévu. Réessayez plus tard : un diagnostic identique ne créera pas une nouvelle issue."), false);
        }
        catch (Exception)
        {
            ShowDiagnosticResult(Tr("Impossible d’envoyer le diagnostic. Vérifiez votre connexion Internet et réessayez. Vous pouvez enregistrer votre profil localement."), false);
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
