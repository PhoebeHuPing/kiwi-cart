namespace KiwiCart.Core.Interfaces;

public interface IPriceCalculator
{
    string CalculateUnitPrice(string productName, decimal price);
}
