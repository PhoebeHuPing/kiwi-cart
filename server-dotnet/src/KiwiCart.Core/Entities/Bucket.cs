namespace KiwiCart.Core.Entities;

public class Bucket
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<BucketItem> Items { get; set; } = [];
}
