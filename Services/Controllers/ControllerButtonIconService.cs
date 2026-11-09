using static InputOS.Services.LocalizationService;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Path = System.Windows.Shapes.Path;

namespace InputOS.Services;

// Icônes vectorielles des commandes du testeur, adaptées à la famille de la manette (PlayStation ou Xbox).
public static class ControllerButtonIconService
{
    // ============================================================================
    // FONCTIONNALITÉ : ICÔNES DES COMMANDES
    // ============================================================================

    private const double IconSize = 30;

    private static readonly Brush LightBrush = Freeze("#EDEEF0");

    public static FrameworkElement Create(
        string id,
        string model,
        string name)
    {
        bool playStation =
            model.Contains("Dual", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("PlayStation", StringComparison.OrdinalIgnoreCase);

        bool xbox =
            model.Contains("Xbox", StringComparison.OrdinalIgnoreCase);


        return id switch
        {
            "FaceBottom" or "FaceRight" or "FaceLeft" or "FaceTop" when playStation =>
                PlayStationFace(id),

            "FaceBottom" or "FaceRight" or "FaceLeft" or "FaceTop" when xbox =>
                XboxFace(id, name),

            "DpadUp" or "DpadDown" or "DpadLeft" or "DpadRight" =>
                DpadArrow(id),

            _ =>
                Badge(ShortLabel(id, playStation, xbox, name))
        };
    }

    private static FrameworkElement PlayStationFace(
        string id)
    {
        (string data, string color, bool filled) =
            id switch
            {
                "FaceBottom" => ("M 7,7 L 23,23 M 23,7 L 7,23", "#7CA6E8", false),
                "FaceRight" => ("M 15,5.5 A 9.5,9.5 0 1 1 14.99,5.5 Z", "#E8707D", false),
                "FaceLeft" => ("M 6.5,6.5 L 23.5,6.5 L 23.5,23.5 L 6.5,23.5 Z", "#E7A0D1", false),
                _ => ("M 15,5.5 L 25,23 L 5,23 Z", "#5CD0A5", false)
            };


        Path path =
            new()
            {
                Data = Geometry.Parse(data),
                Stroke = Freeze(color),
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

        return Host(path);
    }

    private static FrameworkElement XboxFace(
        string id,
        string name)
    {
        (string color, string letterColor) =
            id switch
            {
                "FaceBottom" => ("#4CAF50", "#FFFFFF"),
                "FaceRight" => ("#E53935", "#FFFFFF"),
                "FaceLeft" => ("#1E88E5", "#FFFFFF"),
                _ => ("#FBC02D", "#202020")
            };


        Grid grid =
            new()
            {
                Width = IconSize,
                Height = IconSize
            };

        grid.Children.Add(
            new Ellipse
            {
                Fill = Freeze(color)
            });

        grid.Children.Add(
            new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Freeze(letterColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

        return grid;
    }

    private static FrameworkElement DpadArrow(
        string id)
    {
        string data =
            id switch
            {
                "DpadUp" => "M 8,19 L 15,10 L 22,19",
                "DpadDown" => "M 8,11 L 15,20 L 22,11",
                "DpadLeft" => "M 19,7 L 10,15 L 19,23",
                _ => "M 11,7 L 20,15 L 11,23"
            };


        Path path =
            new()
            {
                Data = Geometry.Parse(data),
                Stroke = LightBrush,
                StrokeThickness = 3.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

        return Host(path);
    }

    private static FrameworkElement Badge(
        string label) =>
        new Border
        {
            MinWidth = IconSize,
            Height = 24,
            Padding = new Thickness(7, 0, 7, 0),
            CornerRadius = new CornerRadius(7),
            Background = Freeze("#3A4150"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child =
                new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
        };

    private static string ShortLabel(
        string id,
        bool playStation,
        bool xbox,
        string name) =>
        id switch
        {
            "LeftShoulder" => xbox ? "LB" : "L1",
            "RightShoulder" => xbox ? "RB" : "R1",
            "LeftTrigger" => xbox ? "LT" : "L2",
            "RightTrigger" => xbox ? "RT" : "R2",
            "LeftStick" => "L3",
            "RightStick" => "R3",
            "MenuLeft" => xbox ? "View" : playStation ? "Create" : name,
            "MenuRight" => xbox ? "Menu" : playStation ? "Options" : name,
            "Home" => "PS",
            "Touchpad" => Tr("Pavé"),
            "Microphone" => "Mic",
            _ => name
        };

    private static FrameworkElement Host(
        Path path)
    {
        Canvas canvas =
            new()
            {
                Width = IconSize,
                Height = IconSize
            };

        canvas.Children.Add(
            path);

        return canvas;
    }

    private static Brush Freeze(
        string color)
    {
        SolidColorBrush brush =
            new((Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

        brush.Freeze();

        return brush;
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : ICÔNES DES COMMANDES
    // ============================================================================
}
