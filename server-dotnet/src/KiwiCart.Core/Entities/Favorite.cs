namespace KiwiCart.Core.Entities;

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
}
