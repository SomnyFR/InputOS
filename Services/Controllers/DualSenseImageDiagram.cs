using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace InputOS.Services;

// Schéma DualSense en calques d'images (fond + feuilles de sprites), sur le principe de gamepad.e7d.io.
// Les images sont attendues dans Assets/Images/DualSense/ ; sans elles, l'appelant garde le dessin vectoriel
// de ControllerDiagramService.
public sealed class DualSenseImageDiagram : LayeredControllerDiagram
{
    // ============================================================================
    // FONCTIONNALITÉ : SCHÉMA DUALSENSE EN CALQUES D'IMAGES
    // ============================================================================

    private const string Folder =
        "pack://application:,,,/InputOS;component/Assets/Images/DualSense/";

    // Centres du CSS d'origine (touchpad 350×300 en 422,74 ; meta 100×100 en 546,336).
    private static readonly Point TouchpadCenter = new(597, 224);

    private static readonly Point MetaCenter = new(596, 386);

    public static DualSenseImageDiagram? TryCreate(
        string model)
    {
        if (!model.Contains("DualSense", StringComparison.OrdinalIgnoreCase)) return null;

        BitmapSource? baseImage = Load("base-white.png");

        if (baseImage == null) return null;

        return new DualSenseImageDiagram(baseImage);
    }

    private DualSenseImageDiagram(
        BitmapSource baseImage) : base(1200, 700)
    {
        Root.Children.Add(
            new Image { Source = baseImage, Width = Width, Height = Height, Stretch = Stretch.Fill });

        BitmapSource? triggers = Load("triggers.png");
        BitmapSource? bumper = Load("bumper.png");
        BitmapSource? buttons = Load("buttons.png");
        BitmapSource? dpad = Load("dpad.png");
        BitmapSource? select = Load("select.png");
        BitmapSource? start = Load("start.png");
        BitmapSource? sticks = Load("sticks.png");


        // Gâchettes : 99×90, la droite est la seconde moitié de la feuille.
        AddVisibility("LeftTrigger", Sprite(triggers, 99, 90, 0, 0, 299, 38));
        AddVisibility("RightTrigger", Sprite(triggers, 99, 90, 99, 0, 798, 38));

        // Pare-chocs : 200×45, le droit est le miroir du gauche.
        AddVisibility("LeftShoulder", Sprite(bumper, 200, 45, 0, 0, 263, 132));
        Rectangle? rightBumper = Sprite(bumper, 200, 45, 0, 0, 735, 132);
        if (rightBumper != null)
        {
            rightBumper.RenderTransformOrigin = new Point(0.5, 0.5);
            rightBumper.RenderTransform = new ScaleTransform(-1, 1);
        }
        AddVisibility("RightShoulder", rightBumper);

        // Create et Options.
        AddVisibility("MenuLeft", Sprite(select, 26, 43, 0, 0, 397, 175));
        AddVisibility("MenuRight", Sprite(start, 26, 43, 0, 0, 773, 175));

        // Boutons ◯ ✕ □ △ : 55×55.
        AddSprite("FaceBottom", Sprite(buttons, 55, 55, 0, 0, 820, 316));
        AddSprite("FaceRight", Sprite(buttons, 55, 55, -57, 0, 880, 257));
        AddSprite("FaceLeft", Sprite(buttons, 55, 55, -113, 0, 762, 257));
        AddSprite("FaceTop", Sprite(buttons, 55, 55, 55, 0, 820, 197));

        // Croix directionnelle.
        AddSprite("DpadUp", Sprite(dpad, 37, 52, -37, 0, 330, 220));
        AddSprite("DpadDown", Sprite(dpad, 37, 52, 0, 0, 330, 294));
        AddSprite("DpadLeft", Sprite(dpad, 52, 40, 104, 0, 286, 265));
        AddSprite("DpadRight", Sprite(dpad, 52, 40, 52, 0, 359, 265));

        // Sticks (clic L3 / R3).
        AddSprite("LeftStick", Sprite(sticks, 94, 94, 0, 0, 422, 348));
        AddSprite("RightStick", Sprite(sticks, 94, 94, 0, 0, 681, 348));

        // Pavé tactile et bouton PS : calques touchpad.png et meta.png, centrés sur la forme réelle.
        // Sans ces images, contour tracé à la main.
        BitmapSource? touchpad = Load("touchpad.png");
        BitmapSource? meta = Load("meta.png");

        if (touchpad != null)
            AddVisibility("Touchpad", Sprite(touchpad, touchpad.PixelWidth, touchpad.PixelHeight, 0, 0,
                TouchpadCenter.X - touchpad.PixelWidth / 2.0, TouchpadCenter.Y - touchpad.PixelHeight / 2.0));
        else
            AddShape(
                "Touchpad",
                new Polygon
                {
                    Points = new PointCollection { new Point(470, 160), new Point(756, 160), new Point(730, 335), new Point(498, 335) },
                    StrokeLineJoin = PenLineJoin.Round
                },
                0, 0);

        if (meta != null)
            AddVisibility("Home", Sprite(meta, meta.PixelWidth, meta.PixelHeight, 0, 0,
                MetaCenter.X - meta.PixelWidth / 2.0, MetaCenter.Y - meta.PixelHeight / 2.0));
        else
            AddShape("Home", new Ellipse { Width = 64, Height = 64 }, 568, 381);
    }

    private static BitmapSource? Load(
        string fileName)
    {
        try
        {
            Uri uri = new(Folder + fileName, UriKind.Absolute);

            // Lève une exception si l'image n'est pas embarquée dans l'application.
            using (Application.GetResourceStream(uri)?.Stream) { }

            BitmapImage image = new();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : SCHÉMA DUALSENSE EN CALQUES D'IMAGES
    // ============================================================================
}
