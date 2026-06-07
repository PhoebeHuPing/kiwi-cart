namespace KiwiCart.Core.DTOs;

public class BucketComparisonResult
{
    public string StoreName { get; set; } = string.Empty;
    public string StoreBrand { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<BucketItemPrice> Items { get; set; } = [];
}

public class BucketItemPrice
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
