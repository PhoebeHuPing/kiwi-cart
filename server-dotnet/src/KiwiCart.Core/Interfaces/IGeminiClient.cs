namespace KiwiCart.Core.Interfaces;

/// <summary>
/// Low-level client for the Google Gemini generative language API.
/// This is the Phase 0 base abstraction: it takes a fully-formed prompt and
/// returns the model's plain-text output. Higher-level features (ingredient
/// extraction, recommendations, chat) build on top of this in later phases.
/// </summary>
public interface IGeminiClient
{
    /// <summary>
    /// Send a single-turn prompt to Gemini and return the generated text.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the model.</param>
    /// <param name="ct">Cancellation token; cancel to abort in-flight calls and save quota.</param>
    /// <returns>The model's generated text, trimmed. Never null.</returns>
    /// <exception cref="KiwiCart.Core.Exceptions.GeminiApiException">
    /// Thrown when the request fails, the API returns an error status, or the
    /// response cannot be parsed into usable text.
    /// </exception>
    Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default);
}
