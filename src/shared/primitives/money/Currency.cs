namespace Whycespace.Shared.Primitives.Money;

public readonly record struct Currency(string Code)
{
    public static Currency USD => new("USD");
    public static Currency EUR => new("EUR");
    public static Currency GBP => new("GBP");

    public static implicit operator string(Currency c) => c.Code;
    public static implicit operator Currency(string code) => new(code);

    public override string ToString() => Code;
}
