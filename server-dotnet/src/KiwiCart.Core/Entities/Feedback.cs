namespace KiwiCart.Core.Entities;

public class Feedback
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public DateTime CreatedAt { get; set; }
}
