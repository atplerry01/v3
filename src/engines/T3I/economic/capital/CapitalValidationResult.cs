namespace Whycespace.Engines.T3I.Economic.Capital;

public sealed record CapitalValidationResult(
    bool IsValid,
    IReadOnlyList<string> ValidationErrors,
    Guid PoolId,
    decimal Amount,
    string Currency,
    DateTime Timestamp)
{
    public static CapitalValidationResult Valid(Guid poolId, decimal amount, string currency)
    {
        return new CapitalValidationResult(
            IsValid: true,
            ValidationErrors: [],
            PoolId: poolId,
            Amount: amount,
            Currency: currency,
            Timestamp: DateTime.UtcNow);
    }

    public static CapitalValidationResult Invalid(
        Guid poolId, decimal amount, string currency, IReadOnlyList<string> errors)
    {
        return new CapitalValidationResult(
            IsValid: false,
            ValidationErrors: errors,
            PoolId: poolId,
            Amount: amount,
            Currency: currency,
            Timestamp: DateTime.UtcNow);
    }
}