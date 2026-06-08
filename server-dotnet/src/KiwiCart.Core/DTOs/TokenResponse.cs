namespace KiwiCart.Core.DTOs;

public record TokenResponse(string Token, DateTime IssuedAt, DateTime ExpiresAt);
