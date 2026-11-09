using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace InputOS.Services;

// ============================================================================
// FONCTIONNALITÉ : LOCALISATION DE L'INTERFACE
// ============================================================================
// Les textes vivent dans Languages/Strings.<code>.xaml (clés « Settings.* », etc.)
// et sont utilisés dans le XAML via {DynamicResource clé}. Le français est la
// référence : il est toujours chargé, la langue choisie le recouvre, donc une clé
// non traduite retombe sur le français. Pour ajouter une langue : créer
// Strings.<code>.xaml et l'ajouter à Available.

public sealed record LanguageOption(string Code, string Name);

public static class LocalizationService
{
    public const string DefaultLanguage = "fr";

    public static readonly IReadOnlyList<LanguageOption> Available =
    [
        new("fr", "Français"),
        new("en", "English"),
    ];

    // À appeler avant d'ouvrir la première fenêtre ; la langue n'est lue qu'au démarrage.
    public static void Apply(string? code)
    {
        code = Available.Any(l => l.Code == code) ? code! : DefaultLanguage;
        Code = code;

        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Add(Load(DefaultLanguage));
        if (code != DefaultLanguage)
            merged.Add(Load(code));
    }

    // Texte localisé pour le code C# ; la clé elle-même si elle est absente.
    public static string Get(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;

    // Langue active (fixée par Apply, jamais modifiée avant un redémarrage).
    public static string Code { get; private set; } = DefaultLanguage;

    // Tables « texte français -> texte traduit » pour les textes écrits en dur (XAML et C#).
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Phrases = new()
    {
        ["en"] = EnglishPhrases.Table,
    };

    // Traduit un texte français ; le français lui-même si la langue est FR ou si la traduction manque.
    public static string Tr(string fr) =>
        Phrases.TryGetValue(Code, out var table) && table.TryGetValue(fr, out var text) ? text : fr;

    // Idem avec des valeurs {0}, {1}… : le modèle français est la clé.
    public static string Tf(string fr, params object?[] args) =>
        string.Format(Tr(fr), args);

    // Traduit les textes du XAML (TextBlock, Run, boutons, titres, info-bulles) d'une fenêtre.
    // Les textes affectés plus tard par le code passent par Tr/Tf.
    public static void Translate(DependencyObject root)
    {
        if (Code == DefaultLanguage) return;

        switch (root)
        {
            case System.Windows.Controls.TextBlock t: t.Text = Tr(t.Text); break;
            case System.Windows.Documents.Run r: r.Text = Tr(r.Text); break;
            case Window w: w.Title = Tr(w.Title); break;
            case System.Windows.Controls.ContentControl c when c.Content is string s: c.Content = Tr(s); break;
            case System.Windows.Controls.HeaderedItemsControl h when h.Header is string s: h.Header = Tr(s); break;
        }
        if (root is FrameworkElement { ToolTip: string tip } fe) fe.ToolTip = Tr(tip);

        foreach (object child in LogicalTreeHelper.GetChildren(root))
            if (child is DependencyObject d) Translate(d);
    }

    private static ResourceDictionary Load(string code) =>
        new() { Source = new Uri($"/InputOS;component/Languages/Strings.{code}.xaml", UriKind.Relative) };
}

// ============================================================================
// FIN FONCTIONNALITÉ : LOCALISATION DE L'INTERFACE
// ============================================================================
