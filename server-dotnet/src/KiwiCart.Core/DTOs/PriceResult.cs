using System.Text.Json.Serialization;

namespace KiwiCart.Core.DTOs;

public class PriceResult
{
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("supermarket_name")]
    public string StoreName { get; set; } = string.Empty;

    [JsonIgnore]
    public string StoreBrand { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [JsonPropertyName("unit_price")]
    public string UnitPrice { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RetrievedAt { get; set; }
}
