namespace KiwiCart.Core.Entities;

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    
    /// <summary>
    /// The GTIN (barcode) of the product at the time it was favorited.
    /// Used for cross-supermarket product matching instead of keyword search.
    /// </summary>
    public string? Gtin { get; set; }
}
