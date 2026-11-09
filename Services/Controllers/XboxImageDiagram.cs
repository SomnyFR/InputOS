using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace InputOS.Services;

// Schéma Xbox One en calques SVG (fond + feuilles de sprites), sur le principe de gamepad.e7d.io (template xbox-one).
// Les images sont attendues dans Assets/Images/Xbox/ ; sans elles, l'appelant garde le dessin vectoriel de
// ControllerDiagramService. S'applique à tous les modèles « Xbox … » (One, Series S/X, Elite, 360…).
public sealed class XboxImageDiagram : LayeredControllerDiagram
{
    // ============================================================================
    // FONCTIONNALITÉ : SCHÉMA XBOX EN CALQUES SVG
    // ============================================================================

    private const string Folder =
        "pack://application:,,,/InputOS;component/Assets/Images/Xbox/";

    public static XboxImageDiagram? TryCreate(
        string model)
    {
        if (!model.StartsWith("Xbox", StringComparison.OrdinalIgnoreCase))
            return null;

        SvgImage? baseImage = SvgImage.Load(Folder + "base-white.svg");

        if (baseImage == null) return null;

        return new XboxImageDiagram(baseImage);
    }

    private XboxImageDiagram(
        SvgImage baseImage) : base(baseImage.Width, baseImage.Height)
    {
        Root.Children.Add(
            new Image { Source = baseImage.Image, Width = Width, Height = Height, Stretch = Stretch.Fill });

        SvgImage? trigger = SvgImage.Load(Folder + "trigger.svg");
        SvgImage? bumper = SvgImage.Load(Folder + "bumper.svg");
        SvgImage? startSelect = SvgImage.Load(Folder + "start-select.svg");
        SvgImage? buttons = SvgImage.Load(Folder + "buttons.svg");
        SvgImage? stick = SvgImage.Load(Folder + "stick.svg");
        SvgImage? dpad = SvgImage.Load(Folder + "dpad.svg");


        // Gâchettes : 89×122 ; la droite est le miroir de la gauche.
        AddVisibility("LeftTrigger", Sprite(trigger, 89, 122, 0, 0, 151, 0));
        AddVisibility("RightTrigger", Mirrored(Sprite(trigger, 89, 122, 0, 0, 510, 0)));

        // Pare-chocs : 170×61.
        AddVisibility("LeftShoulder", Sprite(bumper, 170, 61, 0, 0, 107, 129));
        AddVisibility("RightShoulder", Mirrored(Sprite(bumper, 170, 61, 0, 0, 473, 129)));

        // Vue (select) et Menu (start) : 33×33.
        AddVisibility("MenuLeft", Sprite(startSelect, 33, 33, 0, 0, 306, 264));
        AddVisibility("MenuRight", Sprite(startSelect, 33, 33, 33, 0, 414, 264));

        // Boutons A B X Y : 53×53. La lettre reste lisible sous le remplissage bleu.
        AddSprite("FaceBottom", Sprite(buttons, 53, 53, 0, 0, 540, 303), glow: .6);
        AddSprite("FaceRight", Sprite(buttons, 53, 53, -53, 0, 590, 253), glow: .6);
        AddSprite("FaceLeft", Sprite(buttons, 53, 53, -106, 0, 490, 253), glow: .6);
        AddSprite("FaceTop", Sprite(buttons, 53, 53, -159, 0, 540, 202), glow: .6);

        // Sticks (clic L3 / R3) : la moitié droite de la feuille est le stick au repos.
        AddSprite("LeftStick", Sprite(stick, 83, 83, -85, 0, 144, 239));
        AddSprite("RightStick", Sprite(stick, 83, 83, -85, 0, 432, 352));

        // Croix directionnelle : chaque direction est un calque invisible au repos.
        AddVisibility("DpadUp", Sprite(dpad, 34, 56, 35, 0, 261, 346));
        AddVisibility("DpadDown", Sprite(dpad, 34, 56, 0, 0, 261, 400));
        AddVisibility("DpadLeft", Sprite(dpad, 56, 34, 0, -93, 223, 384));
        AddVisibility("DpadRight", Sprite(dpad, 56, 34, 0, -57, 277, 384));

        // Guide et Partage : sans calque dans le modèle d'origine, contour bleu à ajuster.
        AddShape("Home", new Ellipse { Width = 44, Height = 44 }, 354, 184);
        AddShape("Capture", new Ellipse { Width = 34, Height = 34 }, 361, 304);

        // Palettes arrière de l'Elite (P1 à P4) : pas de calque, pastilles bleues sous la manette.
        for (int paddle = 0; paddle < 4; paddle++)
            AddShape(
                $"Paddle{paddle + 1}",
                new Rectangle { Width = 60, Height = 22, RadiusX = 11, RadiusY = 11 },
                255 + paddle * 72,
                590);
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
    // FIN FONCTIONNALITÉ : SCHÉMA XBOX EN CALQUES SVG
    // ============================================================================
}
