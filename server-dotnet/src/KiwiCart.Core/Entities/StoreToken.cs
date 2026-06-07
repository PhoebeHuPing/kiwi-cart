namespace KiwiCart.Core.Entities;

public class StoreToken
{
    public int Id { get; set; }
    public string StoreBrand { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
