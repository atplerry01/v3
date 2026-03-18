namespace Whycespace.Shared.Primitives.Money;

public sealed record ExchangeRate(
    Currency From,
    Currency To,
    decimal Rate,
    DateTimeOffset EffectiveAt
)
{
    public Money Convert(Money amount)
    {
        if (amount.Currency != From)
            throw new InvalidOperationException($"Exchange rate is for {From}, not {amount.Currency}");
        return new Money(amount.Amount * Rate, To);
    }
}
