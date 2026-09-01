namespace KiwiCart.Core.DTOs;

public class FeedbackRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
}
