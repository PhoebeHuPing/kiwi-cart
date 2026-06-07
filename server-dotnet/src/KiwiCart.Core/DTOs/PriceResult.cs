namespace KiwiCart.Core.DTOs;

public class PriceResult
{
    public string ProductName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreBrand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime RetrievedAt { get; set; }
}
