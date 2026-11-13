using static InputOS.Services.LocalizationService;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Image = System.Windows.Controls.Image;
using Pen = System.Windows.Media.Pen;
using Color = System.Windows.Media.Color;

namespace InputOS.Services;

// ============================================================================
// FONCTIONNALITÉ : ILLUSTRATION PAR TYPE DE MATÉRIEL
// ============================================================================
// Dessins vectoriels simples (aucune image externe) dans un repère 200 × 120, aux couleurs du thème sombre.
// Les dessins sont volontairement génériques : ils montrent la famille de matériel, pas un modèle précis,
// et ne portent aucun marqueur de commande (les positions réelles sont inconnues).
public static class ControllerTypeIllustrationService
{
    // Mêmes valeurs que App.xaml (AppCardHoverBrush, AppSecondaryTextBrush, AppAccentBrush).
    private static readonly Brush Fill = Frozen(Color.FromRgb(0x2A, 0x2D, 0x35));
    private static readonly Brush Line = Frozen(Color.FromRgb(0x9A, 0xA1, 0xAC));
    private static readonly Brush Accent = Frozen(Color.FromRgb(0x00, 0xC2, 0xFF));

    private static readonly Dictionary<string, ImageSource> cache = new();

    private static Brush Frozen(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    // Texte alternatif traduit (clé française = texte affiché).
    public static string GetDescription(string type) => type switch
    {
        "ArcadeStick" => Tr("Illustration : stick arcade"),
        "Wheel" => Tr("Illustration : volant"),
        "Joystick" => Tr("Illustration : joystick ou HOTAS"),
        "RadioController" => Tr("Illustration : radiocommande ou drone"),
        "Gamepad" => Tr("Illustration : manette de jeu"),
        _ => Tr("Illustration : matériel de jeu")
    };

    // Source d'image d'un type ; la manette de jeu garde l'illustration existante.
    public static ImageSource GetImage(string type)
    {
        if (!cache.TryGetValue(type, out ImageSource? image))
            cache[type] = image = type == "Gamepad" ? LoadPng("xbox-controller.png") : Draw(type);
        return image;
    }

    public static ImageSource LoadPng(string file)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri($"pack://application:,,,/InputOS;component/Assets/Images/{file}");
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static void UpdateImage(Image image, string type)
    {
        image.Source = GetImage(type);
        AutomationProperties.SetName(image, GetDescription(type));
        image.ToolTip = GetDescription(type);
    }

    private static void Shape(DrawingGroup group, string data, Brush? fill, Brush? stroke, double thickness = 2)
    {
        Pen? pen = stroke == null ? null : new Pen(stroke, thickness)
        {
            LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
        };
        group.Children.Add(new GeometryDrawing(fill, pen, Geometry.Parse(data)));
    }

    private static string Circle(double x, double y, double r) =>
        FormattableString.Invariant($"M{x - r},{y} a{r},{r} 0 1 0 {2 * r},0 a{r},{r} 0 1 0 {-2 * r},0 Z");

    private static ImageSource Draw(string type)
    {
        DrawingGroup g = new();
        // Zone transparente fixe : toutes les illustrations ont le même cadre, donc la même échelle.
        g.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 200, 120))));

        switch (type)
        {
            case "ArcadeStick":
                Shape(g, "M22,38 L178,38 Q186,38 186,46 L186,96 Q186,104 178,104 L22,104 Q14,104 14,96 L14,46 Q14,38 22,38 Z", Fill, Line);
                Shape(g, Circle(52, 71, 21), null, Line);
                Shape(g, Circle(52, 71, 13), Accent, null);
                foreach (double x in new[] { 105.0, 130, 155 })
                    foreach (double y in new[] { 59.0, 83 })
                        Shape(g, Circle(x, y, 8), Fill, Line);
                Shape(g, "M168,44 L180,44", null, Line);
                break;

            case "Wheel":
                Shape(g, Circle(100, 60, 46), null, Line, 9);
                Shape(g, Circle(100, 60, 46), null, Fill, 5);
                Shape(g, "M54,60 L86,60 M114,60 L146,60 M100,74 L100,106", null, Line, 6);
                Shape(g, Circle(100, 60, 14), Fill, Line);
                Shape(g, "M93,12 L107,12", null, Accent, 6);
                break;

            case "Joystick":
                // Manche + base, et une manette des gaz à droite (HOTAS).
                Shape(g, "M20,100 L110,100 Q118,100 118,108 L118,112 L12,112 L12,108 Q12,100 20,100 Z", Fill, Line);
                Shape(g, "M58,100 L58,52 M72,100 L72,52", null, Line);
                Shape(g, "M54,52 Q54,12 65,12 Q76,12 76,52 Z", Fill, Line);
                Shape(g, Circle(65, 30, 5), Accent, null);
                Shape(g, "M132,106 L186,106 L186,90 L132,90 Z", Fill, Line);
                Shape(g, "M148,90 L158,32 Q160,26 166,28 Q171,30 170,36 L164,90", Fill, Line);
                Shape(g, Circle(162, 40, 4), Accent, null);
                break;

            case "RadioController":
                Shape(g, "M170,50 L170,10", null, Line, 3);
                Shape(g, "M34,42 Q34,26 52,26 L148,26 Q166,26 166,42 L166,86 Q166,104 148,104 L52,104 Q34,104 34,86 Z", Fill, Line);
                Shape(g, Circle(70, 68, 19), null, Line);
                Shape(g, Circle(70, 68, 8), Accent, null);
                Shape(g, Circle(130, 68, 19), null, Line);
                Shape(g, Circle(130, 68, 8), Accent, null);
                foreach (double x in new[] { 60.0, 78, 122, 140 })
                    Shape(g, $"M{x},40 L{x},32", null, Line, 4);
                Shape(g, "M94,40 L106,40", null, Line, 4);
                break;

            default: // "Other" : silhouette neutre (boîtier, bouton, curseur et axe).
                Shape(g, "M30,34 Q30,22 42,22 L158,22 Q170,22 170,34 L170,86 Q170,98 158,98 L42,98 Q30,98 30,86 Z", Fill, Line);
                Shape(g, Circle(66, 60, 16), null, Line);
                Shape(g, Circle(66, 60, 7), Accent, null);
                Shape(g, "M104,48 L156,48 M104,72 L156,72", null, Line, 3);
                Shape(g, Circle(122, 48, 5), Accent, null);
                Shape(g, Circle(140, 72, 5), Line, null);
                break;
        }

        g.Freeze();
        DrawingImage image = new(g);
        image.Freeze();
        return image;
    }
}
// ============================================================================
// FIN FONCTIONNALITÉ : ILLUSTRATION PAR TYPE DE MATÉRIEL
// ============================================================================
