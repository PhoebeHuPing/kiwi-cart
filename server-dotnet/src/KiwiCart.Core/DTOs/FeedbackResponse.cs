namespace KiwiCart.Core.DTOs;

/// <summary>
/// Public-facing feedback shape. Intentionally excludes <c>UserId</c> so the
/// public GET endpoint does not leak user identifiers.
/// </summary>
public class FeedbackResponse
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public DateTime CreatedAt { get; set; }
}
