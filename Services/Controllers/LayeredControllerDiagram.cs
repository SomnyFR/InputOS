using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace InputOS.Services;

// Base des schémas de manette en calques d'images (principe de gamepad.e7d.io) : un fond, des feuilles de sprites
// découpées à la position CSS d'origine, et un remplissage bleu pour chaque commande demandée ou appuyée.
public abstract class LayeredControllerDiagram
{
    // ============================================================================
    // FONCTIONNALITÉ : SCHÉMA EN CALQUES D'IMAGES
    // ============================================================================

    public double Width { get; }

    public double Height { get; }

    public Canvas Root { get; }

    // Identifiant de commande (FaceBottom, DpadUp, LeftTrigger…) -> allume ou éteint la commande.
    public Dictionary<string, Action<bool>> Highlighters { get; } = new();

    protected static readonly Brush Accent = Freeze(Color.FromRgb(0x00, 0xC2, 0xFF));

    protected LayeredControllerDiagram(
        double width,
        double height)
    {
        Width = width;
        Height = height;
        Root = new Canvas { Width = width, Height = height };
    }

    protected static Brush Freeze(
        Color color)
    {
        SolidColorBrush brush = new(color);

        brush.Freeze();

        return brush;
    }

    // Découpe w×h de la feuille à la position CSS (cssX, cssY) : un décalage CSS négatif avance dans l'image,
    // un décalage positif revient depuis la fin, car le site répète l'image.
    protected static Rectangle? Sprite(
        BitmapSource? image,
        double width,
        double height,
        double cssX,
        double cssY,
        double left,
        double top) =>
        image == null
            ? null
            : Sprite(image, image.PixelWidth, image.PixelHeight, width, height, cssX, cssY, left, top);

    protected static Rectangle? Sprite(
        ImageSource? image,
        double sourceWidth,
        double sourceHeight,
        double width,
        double height,
        double cssX,
        double cssY,
        double left,
        double top)
    {
        if (image == null) return null;

        double x = Math.Min(Mod(-cssX, sourceWidth), Math.Max(0, sourceWidth - width));
        double y = Math.Min(Mod(-cssY, sourceHeight), Math.Max(0, sourceHeight - height));

        Rectangle rectangle =
            new()
            {
                Width = width,
                Height = height,
                Fill = new ImageBrush(image)
                {
                    ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                    Viewbox = new Rect(x / sourceWidth, y / sourceHeight, width / sourceWidth, height / sourceHeight),
                    Stretch = Stretch.Fill
                }
            };

        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);

        return rectangle;
    }

    // Même silhouette que la découpe d'origine, remplie en bleu : toutes les commandes s'allument de la même couleur.
    private static Rectangle Blue(
        Rectangle sprite)
    {
        Rectangle blue =
            new()
            {
                Width = sprite.Width,
                Height = sprite.Height,
                Fill = Accent,
                OpacityMask = ((ImageBrush)sprite.Fill).Clone(),
                RenderTransformOrigin = sprite.RenderTransformOrigin,
                RenderTransform = sprite.RenderTransform,
                Opacity = 0
            };

        Canvas.SetLeft(blue, Canvas.GetLeft(sprite));
        Canvas.SetTop(blue, Canvas.GetTop(sprite));

        return blue;
    }

    // Calque invisible au repos (gâchette, pare-chocs, Create…) : il apparaît en bleu quand la commande est active.
    protected void AddVisibility(
        string id,
        Rectangle? layer,
        double glow = 1)
    {
        if (layer == null) return;

        Rectangle blue = Blue(layer);

        Root.Children.Add(blue);
        Highlighters[id] = active => blue.Opacity = active ? glow : 0;
    }

    // Calque toujours visible (bouton, stick) : un remplissage bleu s'ajoute par-dessus quand la commande est active.
    protected void AddSprite(
        string id,
        Rectangle? idle,
        double glow = 1)
    {
        if (idle == null) return;

        Rectangle pressed = Blue(idle);

        Root.Children.Add(idle);
        Root.Children.Add(pressed);
        Highlighters[id] = active => pressed.Opacity = active ? glow : 0;
    }

    protected void AddShape(
        string id,
        Shape shape,
        double left,
        double top)
    {
        shape.Stroke = Accent;
        shape.StrokeThickness = 6;
        shape.Fill = Freeze(Color.FromArgb(0x70, 0x00, 0xC2, 0xFF));
        shape.Opacity = 0;

        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);

        Root.Children.Add(shape);
        Highlighters[id] = active => shape.Opacity = active ? 1 : 0;
    }

    private static double Mod(
        double value,
        double modulus) =>
        ((value % modulus) + modulus) % modulus;

    // ============================================================================
    // FIN FONCTIONNALITÉ : SCHÉMA EN CALQUES D'IMAGES
    // ============================================================================
}
