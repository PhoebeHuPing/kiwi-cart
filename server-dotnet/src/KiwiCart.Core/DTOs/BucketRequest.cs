namespace KiwiCart.Core.DTOs;

public class BucketRequest
{
    public List<BucketRequestItem> Items { get; set; } = [];
}

public class BucketRequestItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
