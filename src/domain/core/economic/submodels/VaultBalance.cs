namespace Whycespace.Domain.Core.Vault;

public sealed record VaultBalance
{
    public decimal Amount { get; }
    public string Currency { get; }

    public VaultBalance(decimal amount, string currency)
    {
        if (amount < 0)
            throw new InvalidOperationException("Vault balance cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("Currency must be specified.");

        Amount = amount;
        Currency = currency;
    }

    public VaultBalance WithAmount(decimal newAmount) => new(newAmount, Currency);
}
