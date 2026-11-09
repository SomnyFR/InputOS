using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace InputOS.Services;

// Schéma DualShock 4 en calques SVG (fond + feuilles de sprites), sur le principe de gamepad.e7d.io (template ds4).
// Les images sont attendues dans Assets/Images/DualShock4/ ; sans elles, l'appelant garde le dessin vectoriel
// de ControllerDiagramService.
public sealed class DualShock4ImageDiagram : LayeredControllerDiagram
{
    // ============================================================================
    // FONCTIONNALITÉ : SCHÉMA DUALSHOCK 4 EN CALQUES SVG
    // ============================================================================

    private const string Folder =
        "pack://application:,,,/InputOS;component/Assets/Images/DualShock4/";

    public static DualShock4ImageDiagram? TryCreate(
        string model)
    {
        if (!string.Equals(model, "DualShock 4", StringComparison.OrdinalIgnoreCase)) return null;

        SvgImage? baseImage = SvgImage.Load(Folder + "base-white.svg");

        if (baseImage == null) return null;

        return new DualShock4ImageDiagram(baseImage);
    }

    private DualShock4ImageDiagram(
        SvgImage baseImage) : base(baseImage.Width, baseImage.Height)
    {
        Root.Children.Add(
            new Image { Source = baseImage.Image, Width = Width, Height = Height, Stretch = Stretch.Fill });

        SvgImage? triggers = SvgImage.Load(Folder + "triggers.svg");
        SvgImage? bumper = SvgImage.Load(Folder + "bumper.svg");
        SvgImage? touchpad = SvgImage.Load(Folder + "touchpad.svg");
        SvgImage? meta = SvgImage.Load(Folder + "meta.svg");
        SvgImage? start = SvgImage.Load(Folder + "start.svg");
        SvgImage? buttons = SvgImage.Load(Folder + "buttons.svg");
        SvgImage? sticks = SvgImage.Load(Folder + "sticks.svg");
        SvgImage? dpad = SvgImage.Load(Folder + "dpad.svg");


        // Gâchettes : 99×91, la droite est la seconde moitié de la feuille. Le CSS n'indique pas de position
        // verticale : elles sont posées en haut du schéma, juste au-dessus des pare-chocs.
        AddVisibility("LeftTrigger", Sprite(triggers, 99, 90.8, 0, 0, 109, 0));
        AddVisibility("RightTrigger", Sprite(triggers, 99, 90.8, 99, 0, 598, 0));

        // Pare-chocs : 99×23, le droit est le miroir du gauche.
        AddVisibility("LeftShoulder", Sprite(bumper, 98.9, 22.4, 0, 0, 109, 94));
        AddVisibility("RightShoulder", Mirrored(Sprite(bumper, 98.9, 22.4, 0, 0, 598, 94)));

        // Pavé tactile et bouton PS : centrés dans leur zone du CSS (262×151 en 272,122 ; 42×42 en 382,340).
        AddVisibility("Touchpad", Sprite(touchpad, 263.9, 151.2, 0, 0, 271.05, 121.9));
        AddVisibility("Home", Sprite(meta, 42.3, 42.3, 0, 0, 381.85, 339.85));

        // Share et Options : 28×46, Options est la seconde moitié de la feuille.
        AddVisibility("MenuLeft", Sprite(start, 28, 46.7, 0, 0, 227, 142));
        AddVisibility("MenuRight", Sprite(start, 28, 46.7, 28, 0, 551, 142));

        // Boutons ✕ ◯ □ △ : 56×56, toujours visibles ; le bleu s'ajoute quand la commande est active.
        AddSprite("FaceBottom", Sprite(buttons, 56, 56, 0, 0, 623, 273), .6);
        AddSprite("FaceRight", Sprite(buttons, 56, 56, -56, 0, 681, 215), .6);
        AddSprite("FaceLeft", Sprite(buttons, 56, 56, 112, 0, 567, 215), .6);
        AddSprite("FaceTop", Sprite(buttons, 56, 56, 56, 0, 623, 159), .6);

        // Sticks (clic L3 / R3).
        AddSprite("LeftStick", Sprite(sticks, 94, 94, 0, 0, 228, 308));
        AddSprite("RightStick", Sprite(sticks, 94, 94, 0, 0, 484, 308));

        // Croix directionnelle.
        AddSprite("DpadUp", Sprite(dpad, 36, 52, -37, 0, 136, 181));
        AddSprite("DpadDown", Sprite(dpad, 36, 52, 0, 0, 136, 255));
        AddSprite("DpadLeft", Sprite(dpad, 52, 36, 104, 0, 92, 225));
        AddSprite("DpadRight", Sprite(dpad, 52, 36, 52, 0, 165, 225));
    }

    private static Rectangle? Mirrored(
        Rectangle? layer)
    {
        if (layer == null) return null;

        layer.RenderTransformOrigin = new Point(0.5, 0.5);
        layer.RenderTransform = new ScaleTransform(-1, 1);

        return layer;
    }

    private static Rectangle? Sprite(
        SvgImage? svg,
        double width,
        double height,
        double cssX,
        double cssY,
        double left,
        double top) =>
        svg == null
            ? null
            : Sprite(svg.Image, svg.Width, svg.Height, width, height, cssX, cssY, left, top);

    // ============================================================================
    // FIN FONCTIONNALITÉ : SCHÉMA DUALSHOCK 4 EN CALQUES SVG
    // ============================================================================
}
