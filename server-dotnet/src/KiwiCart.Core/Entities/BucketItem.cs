namespace KiwiCart.Core.Entities;

public class BucketItem
{
    public int Id { get; set; }
    public int BucketId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
