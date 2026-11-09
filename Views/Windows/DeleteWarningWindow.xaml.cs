using static InputOS.Services.LocalizationService;
using System.Windows;

namespace InputOS;

public partial class DeleteWarningWindow : Window
{

    // ============================================================================
    // FONCTIONNALITÉ : AVERTISSEMENT AVANT SUPPRESSION
    // ============================================================================

    // ---------------------------------------------------------------------------
    // LOGIQUE
    // ---------------------------------------------------------------------------

    public bool DoNotShowAgain => DoNotShowCheckBox.IsChecked == true;

    public DeleteWarningWindow(string what)
    {
        InitializeComponent();

        MessageText.Text = Tf("Vous allez supprimer : {0}.", what);
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    // ============================================================================
    // FIN FONCTIONNALITÉ : AVERTISSEMENT AVANT SUPPRESSION
    // ============================================================================

}
