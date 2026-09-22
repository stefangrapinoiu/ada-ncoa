using System.Globalization;
using System.Text;

namespace DaMaiDeparte.Web.Infrastructure;

public enum ProhibitedFoodGroup
{
    None = 0,
    Meat = 1,
    Dairy = 2
}

/// <summary>
/// A secondary guard against meat and dairy listings.
///
/// The <em>primary</em> control is the category allowlist: a donation can only be created in a
/// category flagged <c>IsAllowed</c>, and no meat or dairy category is allowed. This screen exists
/// because a user could still type "salam" into a bakery listing, and because the rules must live
/// in the business layer rather than only being hidden in the UI.
///
/// It is deliberately conservative and deliberately not the only control: it is a keyword filter,
/// not a moderation system.
/// </summary>
public static class ProhibitedFoodScreen
{
    private static readonly string[] MeatTerms =
    {
        "carne", "carnati", "carnat", "carnaciori", "mezel", "mezeluri", "salam", "sunca",
        "parizer", "crenvursti", "crenvurst", "pastrama", "slanina", "bacon", "jambon",
        "pate", "pui", "porc", "vita", "vitel", "miel", "oaie", "curcan", "rata", "gasca",
        "kaizer", "cabanos", "chorizo", "prosciutto", "salami", "peste", "pestisor",
        "ton", "somon", "macrou", "hering", "sardine", "sardele", "anchoa",
        "creveti", "crevete", "midii", "scoici", "calamar", "caracatita", "homar", "crab",
        "fructe de mare", "icre", "sushi"
    };

    private static readonly string[] DairyTerms =
    {
        "lapte", "lactat", "lactate", "iaurt", "branza", "branzeturi", "cascaval", "telemea",
        "urda", "smantana", "unt", "kefir", "chefir", "sana", "frisca", "mozzarella",
        "parmezan", "cheddar", "camembert", "gorgonzola", "feta", "ricotta", "mascarpone",
        "cottage", "zer"
    };

    /// <summary>
    /// Plant-based products that legitimately reuse a dairy word. Matched on the normalized
    /// (diacritic-free, lower-case) text.
    /// </summary>
    private static readonly string[] PlantBasedPhrases =
    {
        "unt de arahide", "unt de migdale", "unt de caju", "unt de cocos", "unt de susan",
        "lapte de migdale", "lapte de soia", "lapte de cocos", "lapte de ovaz", "lapte de orez",
        "lapte de alune", "lapte vegetal", "bautura vegetala",
        "iaurt vegetal", "iaurt de soia", "iaurt de cocos",
        "branza vegana", "branza vegetala", "smantana vegetala", "unt vegetal"
    };

    /// <summary>
    /// Returns the prohibited group detected in the supplied free text, or
    /// <see cref="ProhibitedFoodGroup.None"/>.
    /// </summary>
    public static ProhibitedFoodGroup Detect(params string?[] texts)
    {
        var haystack = Normalize(string.Join(' ', texts.Where(t => !string.IsNullOrWhiteSpace(t))));
        if (haystack.Length == 0)
        {
            return ProhibitedFoodGroup.None;
        }

        // Plant-based products borrow dairy words ("unt de arahide", "lapte de migdale").
        // They are allowed, so those phrases are removed before the keyword pass runs.
        foreach (var phrase in PlantBasedPhrases)
        {
            haystack = haystack.Replace(phrase, " ", StringComparison.Ordinal);
        }

        haystack = string.Join(' ', haystack.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (ContainsAny(haystack, MeatTerms))
        {
            return ProhibitedFoodGroup.Meat;
        }

        return ContainsAny(haystack, DairyTerms) ? ProhibitedFoodGroup.Dairy : ProhibitedFoodGroup.None;
    }

    private static bool ContainsAny(string haystack, string[] terms) =>
        terms.Any(term => ContainsWord(haystack, term));

    /// <summary>
    /// Romanian plural and article endings. A term only matches when it is a whole word or is
    /// followed by one of these, so "salamuri" and "puiul" are caught while "tonic" is not
    /// mistaken for "ton" and "casetă" is not mistaken for a cheese.
    /// </summary>
    private static readonly string[] WordEndings =
    {
        "a", "e", "i", "l", "ul", "ii", "le", "ei", "lor", "uri", "ului", "elor", "urile"
    };

    /// <summary>
    /// Whole-word match so "cascaval" is caught but "pui" does not fire on "pulpe de dovleac".
    /// The haystack is padded with spaces so the first and last words behave like the rest.
    /// </summary>
    private static bool ContainsWord(string haystack, string term)
    {
        var padded = $" {haystack} ";
        if (padded.Contains($" {term} ", StringComparison.Ordinal))
        {
            return true;
        }

        return WordEndings.Any(ending => padded.Contains($" {term}{ending} ", StringComparison.Ordinal));
    }

    /// <summary>Lower-cases, removes Romanian diacritics and collapses punctuation to spaces.</summary>
    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // ș/ț with comma below and with cedilla both decompose differently, so replace them first.
        var replaced = value
            .Replace('ș', 's').Replace('Ș', 'S')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ț', 't').Replace('Ț', 'T')
            .Replace('ţ', 't').Replace('Ţ', 'T')
            .ToLower(RoDate.Culture);

        var decomposed = replaced.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
