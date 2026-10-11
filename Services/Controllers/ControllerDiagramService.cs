using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InputOS.Models;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

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
        bool playStation = family == "PlayStation";
        bool xbox = family == "Xbox";
        string outline = family switch
        {
            "JoyCon" => "M65,90 Q65,70 90,70 L150,70 Q175,70 175,95 L175,280 Q175,310 150,310 L90,310 Q65,310 65,280 Z M305,95 Q305,70 330,70 L390,70 Q415,70 415,95 L415,280 Q415,310 390,310 L330,310 Q305,310 305,280 Z",
            "Nintendo" => "M105,85 Q135,65 175,85 L305,85 Q345,65 375,85 Q400,100 420,160 L450,265 Q455,310 410,300 L345,255 L135,255 L70,300 Q25,310 30,265 L60,160 Q80,100 105,85 Z",
            "Xbox" => "M128,75 C108,75 94,79 85,91 C68,110 53,161 43,204 C32,247 19,300 43,312 C64,324 90,308 113,287 C145,259 169,248 197,248 C221,246 259,246 283,248 C311,248 335,259 367,287 C390,308 416,324 437,312 C461,300 448,247 437,204 C427,161 412,110 395,91 C386,79 372,75 352,75 C280,70 200,70 128,75 Z",
            _ when model == "DualShock 4" => "M100,85 Q140,65 175,85 L305,85 Q340,65 380,85 L420,175 L450,265 Q455,315 415,300 L345,255 L135,255 L65,300 Q25,315 30,265 L60,175 Z",
            _ => "M110,80 Q150,70 180,80 L300,80 Q330,70 370,80 Q405,95 425,170 L450,260 Q460,315 415,300 Q390,290 345,250 L135,250 Q90,290 65,300 Q20,315 30,260 L55,170 Q75,95 110,80 Z"
        };
        Geometry bodyGeometry = Geometry.Parse(outline);
        if (xbox)
        {
            bodyGeometry = bodyGeometry.Clone();
            bodyGeometry.Transform = new MatrixTransform(.9, 0, 0, 1, 24, 0);
        }
        if (playStation)
        {
            // Contour du SVG fourni, ramené au repère 480 × 330 de la page.
            var geometry = Geometry.Parse("M220.5,92 C200.5,92 154,92 128,92 C95.5,92 66.5,109.5 55,137.5 C43.5,165.5 4,271.1 4,317.5 C4,363.9 17.5,378.5 49.5,378.5 C81.5,378.5 105,294.5 150,294.5 L290.5,294.5 C335.5,294.5 359,378.5 391,378.5 C423,378.5 436.5,363.9 436.5,317.5 C436.5,271.1 397,165.5 385.5,137.5 C374,109.5 345,92 312.5,92 Z");
            geometry = geometry.Clone();
            geometry.Transform = new MatrixTransform(.98, 0, 0, .78, 24, 8);
            bodyGeometry = geometry;
        }
        // Relief discret, comme une manette posée sur la surface du testeur.
        var shadow = new System.Windows.Shapes.Path
        {
            Data = bodyGeometry, Opacity = .35,
            RenderTransform = new TranslateTransform(0, 10), IsHitTestVisible = false
        };
        shadow.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "AppBorderBrush");
        canvas.Children.Add(shadow);
        var body = new System.Windows.Shapes.Path
        {
            Data = bodyGeometry, StrokeThickness = 2,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(195, 215, 238))
        };
        body.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "AppPanelBrush");
        canvas.Children.Add(body);

        if (!playStation && !xbox && family != "JoyCon")
        {
            // Jonctions des poignées et panneau inférieur : détails purement visuels.
            AddContour(canvas, "M55,170 L135,240 Q160,225 185,225 L295,225 Q320,225 345,240 L425,170", 1.5, .45);
            AddContour(canvas, "M70,288 Q90,280 135,242 L345,242 Q390,280 410,288", 3, .35);
        }

        bool symmetric = family == "PlayStation";
        const double playStationLeftGroupX = 128;
        const double playStationRightGroupX = 352;
        const double playStationGroupY = 145;
        AddStickOutline(canvas, symmetric ? 187 : xbox ? 127.5 : 115, symmetric ? 194 : 135);
        AddStickOutline(canvas, symmetric ? 296 : family == "JoyCon" ? 365 : xbox ? 294 : 300, symmetric ? 194 : xbox ? 199 : 215);
        AddButtonWell(canvas, symmetric ? playStationRightGroupX : xbox ? 346 : 365,
            symmetric ? playStationGroupY : xbox ? 135 : 133, symmetric ? 74 : xbox ? 78 : 72);
        AddButtonWell(canvas, symmetric ? playStationLeftGroupX : family == "JoyCon" ? 115 : xbox ? 168 : 160,
            symmetric ? playStationGroupY : xbox ? 199 : 215, symmetric || xbox ? 68 : 76);

        foreach (var definition in definitions)
        {
            bool shoulder = definition.Id.Contains("Shoulder");
            bool stick = definition.Id is "LeftStick" or "RightStick";
            bool petal = playStation && definition.IsDirection;
            bool face = playStation && definition.Id.StartsWith("Face");
            double width = definition.IsTrigger ? (playStation ? 48 : 28) : shoulder ? 48 : definition.Id == "Touchpad" ? 80 : stick ? 46 : 30;
            double height = definition.IsTrigger ? 32 : shoulder ? 16 : stick ? 46 : 30;
            var label = new TextBlock
            {
                Text = petal || face || (playStation && stick) ? "" : definition.Label, FontSize = definition.Label.Length > 2 ? 10 : 14,
                FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var marker = new DiagramButtonBorder
            {
                Width = width, Height = height, BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(definition.Id == "Touchpad" || definition.IsTrigger ? 7 : height / 2),
                Child = label, ToolTip = definition.Name
            };
            double x = definition.X, y = definition.Y;
            if (xbox)
            {
                x = 240 + (x - 240) * .9;
                if (stick)
                {
                    label.Text = "";
                    if (definition.Id == "RightStick") y = 199;
                }
                if (definition.IsTrigger) { marker.Width = width = 48; }
                if (definition.Id.StartsWith("Face"))
                {
                    double angle = definition.Id switch { "FaceTop" => 0, "FaceRight" => 90, "FaceBottom" => 180, _ => 270 };
                    x = 346 + Math.Sin(angle * Math.PI / 180) * 23;
                    y = 135 - Math.Cos(angle * Math.PI / 180) * 23;
                    marker.Width = marker.Height = width = height = 26;
                    marker.CornerRadius = new CornerRadius(13);
                    var color = definition.Id switch
                    {
                        "FaceBottom" => System.Windows.Media.Color.FromRgb(84,157,60),
                        "FaceRight" => System.Windows.Media.Color.FromRgb(211,70,65),
                        "FaceLeft" => System.Windows.Media.Color.FromRgb(30,149,196),
                        _ => System.Windows.Media.Color.FromRgb(201,177,38)
                    };
                    marker.Tag = new SolidColorBrush(color);
                    label.Foreground = (Brush)marker.Tag;
                    label.FontSize = 16;
                }
                if (definition.IsDirection)
                {
                    double angle = definition.Id switch { "DpadUp" => 0, "DpadRight" => 90, "DpadDown" => 180, _ => 270 };
                    x = 168 + Math.Sin(angle * Math.PI / 180) * 18;
                    y = 199 - Math.Cos(angle * Math.PI / 180) * 18;
                    marker.Width = marker.Height = width = height = 23;
                    label.Text = "";
                    marker.ButtonShape = Geometry.Parse("M5,0 L18,0 Q23,0 23,5 L23,13 Q23,16 20,19 L13.5,26 Q11.5,28 9.5,26 L3,19 Q0,16 0,13 L0,5 Q0,0 5,0 Z").Clone();
                    marker.ButtonShape.Transform = new RotateTransform(angle,11.5,11.5);
                    marker.SymbolShape = Geometry.Parse("M8.5,10 L11.5,7 L14.5,10 M11.5,7 L11.5,14").Clone();
                    marker.SymbolShape.Transform = new RotateTransform(angle,11.5,11.5);
                    marker.SetBinding(DiagramButtonBorder.SymbolBrushProperty,
                        new System.Windows.Data.Binding("Foreground") { Source = label });
                }
                if (definition.Id is "MenuLeft" or "MenuRight" or "Capture" or "Home")
                {
                    label.Text = "";
                    marker.Width = width = definition.Id == "Capture" ? 28 : definition.Id == "Home" ? 34 : 24;
                    marker.Height = height = definition.Id == "Capture" ? 16 : width;
                    marker.CornerRadius = new CornerRadius(height / 2);
                    if (definition.Id == "Capture") y = 170;
                    marker.SymbolShape = definition.Id switch
                    {
                        "MenuLeft" => Geometry.Parse("M6,6 L14,6 L14,14 L6,14 Z M10,10 L18,10 L18,18 L10,18 Z"),
                        "MenuRight" => Geometry.Parse("M6,7 L18,7 M6,12 L18,12 M6,17 L18,17"),
                        "Capture" => Geometry.Parse("M8,8 L8,12 L20,12 L20,8 M14,10 L14,3 M11,6 L14,3 L17,6"),
                        _ => Geometry.Parse("M7,7 Q17,11 27,27 M27,7 Q17,11 7,27")
                    };
                    marker.SetBinding(DiagramButtonBorder.SymbolBrushProperty,
                        new System.Windows.Data.Binding("Foreground") { Source = label });
                }
            }
            if (playStation)
            {
                if (shoulder || definition.IsTrigger) x = definition.Id.StartsWith("Left") ? 160 : 321;
                if (stick) { x = definition.Id == "LeftStick" ? 187 : 296; y = 194; }
                if (definition.Id == "Touchpad")
                {
                    marker.Width = width = 150;
                    marker.Height = height = 60;
                    x = 240; y = 114;
                    label.Text = "";
                    marker.ButtonShape = Geometry.Parse("M8,0 Q75,-3 142,0 Q150,0 148,9 L141,43 Q138,58 121,60 L29,60 Q12,58 9,43 L2,9 Q0,0 8,0 Z");
                }
                if (definition.Id is "MenuLeft" or "MenuRight")
                {
                    marker.Width = width = 10;
                    marker.Height = height = 22;
                    marker.CornerRadius = new CornerRadius(5);
                    bool leftMenu = definition.Id == "MenuLeft";
                    x = leftMenu ? 156 : 324;
                    y = 99;
                    double tilt = leftMenu ? -12 : 12;
                    marker.RenderTransformOrigin = new Point(.5, .5);
                    marker.RenderTransform = new RotateTransform(tilt);
                    label.Text = "";
                }
                if (definition.Id == "Microphone")
                {
                    marker.Width = width = 30;
                    marker.Height = height = 12;
                    marker.CornerRadius = new CornerRadius(3);
                    label.FontSize = 7;
                    y = 222;
                }
                if (face)
                {
                    double angle = definition.Id switch { "FaceTop" => 0, "FaceRight" => 90, "FaceBottom" => 180, _ => 270 };
                    double radians = angle * Math.PI / 180;
                    x = playStationRightGroupX + Math.Sin(radians) * 21;
                    y = playStationGroupY - Math.Cos(radians) * 21;
                    marker.Width = marker.Height = width = height = 25;
                    marker.CornerRadius = new CornerRadius(12.5);
                    marker.SymbolShape = definition.Id switch
                    {
                        "FaceTop" => Geometry.Parse("M12.5,6.5 L19,18 L6,18 Z"),
                        "FaceRight" => new EllipseGeometry(new Point(12.5,12.5), 5.5, 5.5),
                        "FaceBottom" => Geometry.Parse("M7.5,7.5 L17.5,17.5 M17.5,7.5 L7.5,17.5"),
                        _ => Geometry.Parse("M7.5,7.5 L17.5,7.5 L17.5,17.5 L7.5,17.5 Z")
                    };
                    marker.SetBinding(DiagramButtonBorder.SymbolBrushProperty,
                        new System.Windows.Data.Binding("Foreground") { Source = label });
                }
                if (petal)
                {
                    string direction = definition.Id[4..];
                    double angle = direction switch { "Up" or "Top" => 0, "Right" => 90, "Down" or "Bottom" => 180, _ => 270 };
                    double radians = angle * Math.PI / 180;
                    x = playStationLeftGroupX + Math.Sin(radians) * 18;
                    y = playStationGroupY - Math.Cos(radians) * 18;
                    marker.Width = marker.Height = width = height = 23;
                    // Même touche arrondie tournée de 90° pour chaque direction.
                    marker.ButtonShape = Geometry.Parse("M5,0 L18,0 Q23,0 23,5 L23,13 Q23,16 20,19 L13.5,26 Q11.5,28 9.5,26 L3,19 Q0,16 0,13 L0,5 Q0,0 5,0 Z");
                    marker.ButtonShape = marker.ButtonShape.Clone();
                    marker.ButtonShape.Transform = new RotateTransform(angle, 11.5, 11.5);
                    marker.SymbolShape = Geometry.Parse("M8.5,10 L11.5,7 L14.5,10 M11.5,7 L11.5,14").Clone();
                    marker.SymbolShape.Transform = new RotateTransform(angle, 11.5, 11.5);
                    marker.SetBinding(DiagramButtonBorder.SymbolBrushProperty,
                        new System.Windows.Data.Binding("Foreground") { Source = label });
                }
            }
            Canvas.SetLeft(marker, x - width / 2);
            Canvas.SetTop(marker, y - height / 2);
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

    private sealed class DiagramButtonBorder : Border
    {
        public Geometry? ButtonShape { get; set; }
        public Geometry? SymbolShape { get; set; }
        public static readonly DependencyProperty SymbolBrushProperty = DependencyProperty.Register(
            nameof(SymbolBrush), typeof(Brush), typeof(DiagramButtonBorder),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        public Brush SymbolBrush
        {
            get => (Brush)GetValue(SymbolBrushProperty);
            set => SetValue(SymbolBrushProperty, value);
        }
        protected override void OnRender(DrawingContext context)
        {
            if (ButtonShape == null) base.OnRender(context);
            else context.DrawGeometry(Background, BorderBrush == null ? null : new System.Windows.Media.Pen(BorderBrush, 1.8), ButtonShape);
            if (SymbolShape != null)
            {
                var pen = new System.Windows.Media.Pen(SymbolBrush, 1.6)
                { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                context.DrawGeometry(null, pen, SymbolShape);
            }
        }
    }

    private static void AddStickOutline(Canvas canvas, double x, double y)
    {
        var circle = new System.Windows.Shapes.Ellipse { Width = 62, Height = 62, StrokeThickness = 2 };
        circle.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(195, 215, 238));
        Canvas.SetLeft(circle, x - 31); Canvas.SetTop(circle, y - 31);
        canvas.Children.Add(circle);
    }

    private static void AddContour(Canvas canvas, string geometry, double thickness, double opacity)
    {
        var line = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry), StrokeThickness = thickness,
            Opacity = opacity, IsHitTestVisible = false,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
        };
        line.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, "AppBorderBrush");
        canvas.Children.Add(line);
    }

    private static void AddButtonWell(Canvas canvas, double x, double y, double size)
    {
        var well = new System.Windows.Shapes.Ellipse
        {
            Width = size, Height = size, StrokeThickness = 1.5, Opacity = .55,
            IsHitTestVisible = false
        };
        well.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(195, 215, 238));
        Canvas.SetLeft(well, x - size / 2); Canvas.SetTop(well, y - size / 2);
        canvas.Children.Add(well);
    }


    // ============================================================================
    // FIN FONCTIONNALITÉ : SILHOUETTE ET COMMANDES PAR FAMILLE DE MANETTE
    // ============================================================================
}
