using System.Text.RegularExpressions;
using KiwiCart.Core.Interfaces;

namespace KiwiCart.Infrastructure.Services;

public partial class PriceCalculator : IPriceCalculator
{
    public string CalculateUnitPrice(string productName, decimal price)
    {
        if (price <= 0) return string.Empty;

        var match = VolumeRegex().Match(productName);
        if (match.Success)
        {
            var value = decimal.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value.ToLowerInvariant();
            return unit switch
            {
                "l" or "ltr" or "litre" or "litres" => FormatPerUnit(price, value, "L"),
                "ml" => FormatPerUnit(price, value / 1000m, "L"),
                "kg" => FormatPer100g(price, value * 1000m),
                "g" => FormatPer100g(price, value),
                _ => string.Empty
            };
        }

        var packMatch = PackRegex().Match(productName);
        if (packMatch.Success)
        {
            var count = decimal.Parse(packMatch.Groups[1].Value);
            if (count > 0)
                return $"${price / count:F2}/ea";
        }

        return string.Empty;
    }

    private static string FormatPerUnit(decimal price, decimal litres, string unit)
        => litres > 0 ? $"${price / litres:F2}/{unit}" : string.Empty;

    private static string FormatPer100g(decimal price, decimal grams)
        => grams > 0 ? $"${price / grams * 100m:F2}/100g" : string.Empty;

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(L|ltr|litre|litres|ml|kg|g)\b", RegexOptions.IgnoreCase)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"(\d+)\s*(?:pk|pack|s)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PackRegex();
}
