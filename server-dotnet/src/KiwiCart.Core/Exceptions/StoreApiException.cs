namespace KiwiCart.Core.Exceptions;

public class StoreApiException(string storeBrand, string message) : Exception(message)
{
    public string StoreBrand { get; } = storeBrand;
}
