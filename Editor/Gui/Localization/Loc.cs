#nullable enable
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Localization;

public enum UiLanguages
{
    English,
    Russian,
}

/// <summary>
/// Translates editor UI strings. The English text itself is the key, so untranslated strings
/// fall back to English and existing call sites stay readable. Shared widgets (menu items, form
/// inputs, tooltips) translate their labels here, which covers most of the UI
/// without touching every caller.
/// </summary>
internal static class Loc
{
    public static UiLanguages Language => UserSettings.Config.UiLanguage;

    public static bool IsEnglish => Language == UiLanguages.English;

    /// <summary>
    /// Returns the translation for the current language or the original text. Allocation-free for
    /// repeated lookups of the same string (literals are interned, so they hit the cache).
    /// </summary>
    public static string T(string? text)
    {
        if (text == null)
            return string.Empty;

        if (text.Length == 0 || IsEnglish)
            return text;

        if (_resolved.TryGetValue(text, out var cached))
            return cached;

        var result = Resolve(text);

        // Interpolated strings would otherwise grow the cache without bound.
        if (_resolved.Count < MaxCacheSize)
            _resolved[text] = result;

        return result;
    }

    /// <summary>
    /// Translated label with the English text kept as ImGui id, so window/menu ids
    /// (and saved layouts) don't change with the language.
    /// </summary>
    public static string WithStableId(string text)
    {
        if (IsEnglish || string.IsNullOrEmpty(text))
            return text;

        if (_stableIdLabels.TryGetValue(text, out var cached))
            return cached;

        var translated = T(text);
        var result = ReferenceEquals(translated, text) ? text : translated + "###" + text;
        if (_stableIdLabels.Count < MaxCacheSize)
            _stableIdLabels[text] = result;

        return result;
    }

    private static string Resolve(string text)
    {
        if (_russian.TryGetValue(text, out var translation))
            return translation;

        // Keep ImGui id suffixes ("Label##id") intact while translating the visible part.
        var idIndex = text.IndexOf("##", StringComparison.Ordinal);
        if (idIndex > 0 && _russian.TryGetValue(text[..idIndex], out var labelTranslation))
            return labelTranslation + text[idIndex..];

        // Tolerate trailing ellipsis and colons ("Save As...", "Name:").
        var trimmed = text.TrimEnd('.', ':', ' ');
        if (trimmed.Length > 0 && trimmed.Length < text.Length && _russian.TryGetValue(trimmed, out var trimmedTranslation))
            return trimmedTranslation + text[trimmed.Length..];

        return text;
    }

    private const int MaxCacheSize = 8192;
    private static readonly Dictionary<string, string> _resolved = new();
    private static readonly Dictionary<string, string> _stableIdLabels = new();
    private static readonly Dictionary<string, string> _russian = LocRussian.Build();
}
