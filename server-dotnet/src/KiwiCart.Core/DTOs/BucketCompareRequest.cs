namespace KiwiCart.Core.DTOs;

public class BucketCompareRequest
{
    public List<BucketItemInput> Items { get; set; } = [];
}

public class BucketItemInput
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}
