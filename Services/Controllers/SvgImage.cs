using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace InputOS.Services;

// Lecteur minimal de SVG embarqués : les éléments <path> (fill, stroke, stroke-width, opacity) sont convertis en
// DrawingImage WPF. Les groupes, dégradés et transformations ne sont pas pris en charge (absents des schémas).
public sealed class SvgImage
{
    // ============================================================================
    // FONCTIONNALITÉ : IMAGES SVG VECTORIELLES
    // ============================================================================

    public DrawingImage Image { get; }

    public double Width { get; }

    public double Height { get; }

    private SvgImage(
        DrawingImage image,
        double width,
        double height)
    {
        Image = image;
        Width = width;
        Height = height;
    }

    public static SvgImage? Load(
        string packUri)
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri(packUri, UriKind.Absolute));

            if (resource == null) return null;

            XDocument document;

            using (resource.Stream)
                document = XDocument.Load(resource.Stream);

            XElement root = document.Root!;
            XNamespace ns = root.Name.Namespace;
            double width = Number(root.Attribute("width")?.Value, 100);
            double height = Number(root.Attribute("height")?.Value, 100);

            // Origine du viewBox : certains SVG ne démarrent pas à (0, 0).
            double[] viewBox = (root.Attribute("viewBox")?.Value ?? "")
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => Number(part, 0))
                .ToArray();
            double minX = viewBox.Length == 4 ? viewBox[0] : 0;
            double minY = viewBox.Length == 4 ? viewBox[1] : 0;

            DrawingGroup content = new() { Transform = new TranslateTransform(-minX, -minY) };

            // Fixe les dimensions de l'image à celles du SVG, même si le contenu déborde.
            content.Children.Add(
                new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(minX, minY, width, height))));

            // Motifs, définitions et masques ne sont pas dessinés : seuls les tracés visibles le sont.
            bool Drawn(XElement element) =>
                !element.Ancestors().Any(ancestor => ancestor.Name.LocalName is "pattern" or "defs" or "clipPath" or "mask");

            foreach (XElement shape in document.Descendants().Where(element =>
                element.Name.LocalName is "path" or "circle" && Drawn(element)))
            {
                Geometry? geometry;

                if (shape.Name.LocalName == "circle")
                {
                    double radius = Number(shape.Attribute("r")?.Value, 0);

                    geometry = new EllipseGeometry(
                        new System.Windows.Point(Number(shape.Attribute("cx")?.Value, 0), Number(shape.Attribute("cy")?.Value, 0)),
                        radius,
                        radius);
                }
                else
                {
                    string? data = shape.Attribute("d")?.Value;

                    if (string.IsNullOrWhiteSpace(data)) continue;

                    // « F1 » : règle de remplissage non nulle, comme SVG (WPF utilise la parité par défaut).
                    geometry = Geometry.Parse("F1 " + data);
                }

                double opacity = Number(shape.Attribute("opacity")?.Value, 1);
                Brush? fill = Paint(shape.Attribute("fill")?.Value, "#000000", opacity);
                Brush? stroke = Paint(shape.Attribute("stroke")?.Value, null, opacity);

                System.Windows.Media.Pen? pen = null;

                if (stroke != null)
                {
                    pen = new System.Windows.Media.Pen(stroke, Number(shape.Attribute("stroke-width")?.Value, 1));
                    pen.Freeze();
                }

                content.Children.Add(new GeometryDrawing(fill, pen, geometry));
            }

            DrawingGroup group = new() { ClipGeometry = new RectangleGeometry(new Rect(0, 0, width, height)) };

            group.Children.Add(content);
            group.Freeze();

            return new SvgImage(new DrawingImage(group), width, height);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Brush? Paint(
        string? value,
        string? fallback,
        double opacity)
    {
        value ??= fallback;

        // « url(#motif) » : motifs non pris en charge, la forme n'est pas remplie.
        if (value == null || value == "none" || value.StartsWith("url(", StringComparison.Ordinal)) return null;

        SolidColorBrush brush =
            new((Color)System.Windows.Media.ColorConverter.ConvertFromString(value)) { Opacity = opacity };

        brush.Freeze();

        return brush;
    }

    private static double Number(
        string? value,
        double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            ? number
            : fallback;

    // ============================================================================
    // FIN FONCTIONNALITÉ : IMAGES SVG VECTORIELLES
    // ============================================================================
}
