using System.Globalization;
using System.Text;

namespace LaMeridienneAPI.Services;

/// <summary>Nettoyage et comparaison des noms de clients (R-08).</summary>
public static class CustomerNames
{
    /// <summary>Retire les espaces superflus : en tête, en fin, et répétés.</summary>
    public static string Clean(string? name) =>
        string.Join(' ', (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Forme de comparaison : sans casse ni accents (« Œuvre Élise » → « oeuvre elise »).</summary>
    public static string Normalize(string? name)
    {
        var decomposed = Clean(name).ToLowerInvariant().Replace("œ", "oe").Replace("æ", "ae").Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
