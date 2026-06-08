using System.Text.Json.Serialization;

namespace KiwiCart.Core.DTOs;

public class BucketCompareResult
{
    [JsonPropertyName("supermarket_name")]
    public string StoreName { get; set; } = string.Empty;

    [JsonPropertyName("total_price")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("items_found")]
    public int ItemsFound { get; set; }

    [JsonPropertyName("missing_items")]
    public List<string> MissingItems { get; set; } = [];

    public List<BucketItemDetail> Details { get; set; } = [];
}

public class BucketItemDetail
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}
