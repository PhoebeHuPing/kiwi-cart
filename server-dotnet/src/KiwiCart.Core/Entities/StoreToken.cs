namespace KiwiCart.Core.Entities;

public class StoreToken
{
    public int Id { get; set; }
    public string StoreBrand { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsNearExpiry => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}
