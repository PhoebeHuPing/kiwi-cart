namespace KiwiCart.Core.Interfaces;

public interface ITokenProvider
{
    string StoreName { get; }
    Task<string> GetTokenAsync(CancellationToken ct = default);
}
