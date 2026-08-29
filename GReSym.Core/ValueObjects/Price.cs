namespace GReSym.Core.ValueObjects;

public record Price
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    
    public Price(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Price cannot be negative");
        
        Amount = amount;
        Currency = currency;
    }
    
    public override string ToString() => $"{Amount} {Currency}";
}