namespace KiwiCart.Core.Entities;

public class Price
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RetrievedAt { get; set; }
}
