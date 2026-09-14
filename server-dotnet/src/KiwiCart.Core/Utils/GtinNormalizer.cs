namespace KiwiCart.Core.Utils;

/// <summary>
/// Helpers for validating and normalizing GTINs (Global Trade Item Numbers).
///
/// Retailers expose GTINs of different lengths for the same catalogue:
/// Foodstuffs "sku" is GTIN-13 for own-brand lines (e.g. 9415077379187) but
/// GTIN-8 for some branded lines (e.g. 94152210); Woolworths "barcode" mirrors
/// the same codes. Padding every GTIN to 14 digits gives a single canonical
/// form so the same product matches across platforms regardless of the
/// original length.
/// </summary>
public static class GtinNormalizer
{
    /// <summary>
    /// Validate and normalize a raw GTIN string to a 14-digit canonical form.
    /// Accepts GTIN-8, GTIN-12, GTIN-13 and GTIN-14 (surrounding whitespace is
    /// trimmed). Returns null when the input is null/empty, not all digits, an
    /// unsupported length, or fails the GTIN check-digit test.
    /// </summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var code = raw.Trim();

        // Must be all digits and a supported GTIN length.
        if (code.Length is not (8 or 12 or 13 or 14))
            return null;
        foreach (var c in code)
        {
            if (c is < '0' or > '9')
                return null;
        }

        if (!HasValidCheckDigit(code))
            return null;

        // Left-pad to the canonical 14-digit form.
        return code.PadLeft(14, '0');
    }

    /// <summary>
    /// True if the string is a structurally valid GTIN (correct length, all
    /// digits, valid check digit). Does not normalize.
    /// </summary>
    public static bool IsValid(string? raw) => Normalize(raw) != null;

    /// <summary>
    /// GTIN check-digit validation. Working right-to-left over all but the last
    /// digit, multiply alternating positions by 3 and 1; the check digit is the
    /// amount needed to round the sum up to a multiple of 10.
    /// </summary>
    private static bool HasValidCheckDigit(string code)
    {
        int sum = 0;
        int weight = 3; // rightmost body digit is weighted 3
        for (int i = code.Length - 2; i >= 0; i--)
        {
            sum += (code[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }
        int expected = (10 - sum % 10) % 10;
        int actual = code[^1] - '0';
        return expected == actual;
    }
}
