using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InputOS.Models;

namespace InputOS.Services;

public static class ControllerDiagramService
{
    // ============================================================================
    // FONCTIONNALITÉ : SILHOUETTE ET COMMANDES PAR FAMILLE DE MANETTE
    // ============================================================================
    public static Dictionary<string, Border> Draw(Canvas canvas, string model, ControllerButtonDefinition[] definitions)
    {
        canvas.Children.Clear();
        Dictionary<string, Border> buttonMarkers = new();
        string family = ControllerButtonCatalogService.GetFamily(model);
        string outline = family switch
        {
            "JoyCon" => "M65,90 Q65,70 90,70 L150,70 Q175,70 175,95 L175,280 Q175,310 150,310 L90,310 Q65,310 65,280 Z M305,95 Q305,70 330,70 L390,70 Q415,70 415,95 L415,280 Q415,310 390,310 L330,310 Q305,310 305,280 Z",
            "Nintendo" => "M105,85 Q135,65 175,85 L305,85 Q345,65 375,85 Q400,100 420,160 L450,265 Q455,310 410,300 L345,255 L135,255 L70,300 Q25,310 30,265 L60,160 Q80,100 105,85 Z",
            "Xbox" => "M105,85 Q145,55 185,85 L295,85 Q335,55 375,85 L410,145 L450,270 Q455,315 410,305 L345,255 L135,255 L70,305 Q25,315 30,270 L70,145 Z",
            _ when model == "DualShock 4" => "M100,85 Q140,65 175,85 L305,85 Q340,65 380,85 L420,175 L450,265 Q455,315 415,300 L345,255 L135,255 L65,300 Q25,315 30,265 L60,175 Z",
            _ => "M110,80 Q150,70 180,80 L300,80 Q330,70 370,80 Q405,95 425,170 L450,260 Q460,315 415,300 Q390,290 345,250 L135,250 Q90,290 65,300 Q20,315 30,260 L55,170 Q75,95 110,80 Z"
        };
        var body = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(outline), StrokeThickness = 2,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(195, 215, 238))
        };
        body.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "AppPanelBrush");
        canvas.Children.Add(body);

        bool symmetric = family == "PlayStation";
        AddStickOutline(canvas, symmetric ? 165 : 115, symmetric ? 215 : 135);
        AddStickOutline(canvas, family == "JoyCon" ? 365 : 300, 215);

        foreach (var definition in definitions)
        {
            double width = definition.IsTrigger || definition.Id.Contains("Shoulder") ? 45 : definition.Id == "Touchpad" ? 80 : 30;
            double height = definition.IsTrigger || definition.Id.Contains("Shoulder") ? 22 : 30;
            var label = new TextBlock
            {
                Text = definition.Label, FontSize = definition.Label.Length > 2 ? 10 : 14,
                FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var marker = new Border
            {
                Width = width, Height = height, BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(definition.Id == "Touchpad" ? 6 : height / 2),
                Child = label, ToolTip = definition.Name
            };
            Canvas.SetLeft(marker, definition.X - width / 2);
            Canvas.SetTop(marker, definition.Y - height / 2);
            buttonMarkers.Add(definition.Id, marker);
            canvas.Children.Add(marker);
        }
        if (definitions.Any(button => button.Id.StartsWith("Paddle") || button.Id.StartsWith("Back")))
        {
            var legend = new TextBlock { Text = "Commandes arrière", FontSize = 11 };
            legend.SetResourceReference(TextBlock.ForegroundProperty, "AppSecondaryTextBrush");
            Canvas.SetLeft(legend, 180); Canvas.SetTop(legend, 312);
            canvas.Children.Add(legend);
        }
        return buttonMarkers;
    }

    private static void AddStickOutline(Canvas canvas, double x, double y)
    {
        var circle = new System.Windows.Shapes.Ellipse { Width = 62, Height = 62, StrokeThickness = 2 };
        circle.SetResourceReference(System.Windows.Shapes.Ellipse.StrokeProperty, "AppBorderBrush");
        Canvas.SetLeft(circle, x - 31); Canvas.SetTop(circle, y - 31);
        canvas.Children.Add(circle);
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : SILHOUETTE ET COMMANDES PAR FAMILLE DE MANETTE
    // ============================================================================
}