using System.Text.Json.Serialization;

namespace KiwiCart.Core.DTOs;

public class PriceResult
{
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("supermarket_name")]
    public string StoreName { get; set; } = string.Empty;

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lng")]
    public double? Lng { get; set; }

    public decimal Price { get; set; }

    [JsonPropertyName("unit_price")]
    public string? UnitPrice { get; set; }

    [JsonIgnore]
    public string StoreBrand { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RetrievedAt { get; set; }
}
