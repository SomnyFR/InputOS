using static InputOS.Services.LocalizationService;
using System.Windows;
using System.Windows.Controls;

namespace InputOS;

// ============================================================================
// FONCTIONNALITÉ : APERÇU DU DIAGNOSTIC AVANT ENVOI
// ============================================================================

// Fenêtre sans XAML : affiche le JSON en lecture seule ; DialogResult vrai = l'utilisateur a choisi d'envoyer.
public sealed class DiagnosticPreviewWindow : Window
{
    public DiagnosticPreviewWindow(string json)
    {
        Title = Tr("Aperçu du diagnostic");
        Width = 640; Height = 560; MinWidth = 420; MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "AppBackgroundBrush");
        SetResourceReference(ForegroundProperty, "AppForegroundBrush");

        var intro = new TextBlock
        {
            Text = Tr("Voici exactement les données qui seront transmises (mises en forme pour la lecture). Une issue publique sera créée sur GitHub, visible par tous. Aucun nom de compte Windows ni identifiant unique du périphérique n’est envoyé."),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)
        };
        var box = new System.Windows.Controls.TextBox
        {
            Text = json, IsReadOnly = true, FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        box.SetResourceReference(BackgroundProperty, "AppPanelBrush");
        box.SetResourceReference(ForegroundProperty, "AppForegroundBrush");
        box.SetResourceReference(BorderBrushProperty, "AppBorderBrush");

        var send = new System.Windows.Controls.Button { Content = Tr("Envoyer"), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = Tr("Annuler"), Padding = new Thickness(16, 8, 16, 8), IsCancel = true };
        send.Click += (_, _) => DialogResult = true;
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(send);
        buttons.Children.Add(cancel);

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(box, 1); Grid.SetRow(buttons, 2);
        grid.Children.Add(intro); grid.Children.Add(box); grid.Children.Add(buttons);
        Content = grid;
    }
}

// ============================================================================
// FIN FONCTIONNALITÉ : APERÇU DU DIAGNOSTIC AVANT ENVOI
// ============================================================================
