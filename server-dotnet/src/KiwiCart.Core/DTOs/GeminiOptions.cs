namespace KiwiCart.Core.DTOs;

/// <summary>
/// Strongly-typed configuration for the Gemini client, bound from the
/// "Gemini" section of appsettings. The API key is supplied out-of-band
/// (dotnet user-secrets locally, Azure App Settings in production) and must
/// never be committed.
/// </summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.5-flash";

    public int MaxOutputTokens { get; set; } = 1024;

    public double Temperature { get; set; } = 0.3;
}
