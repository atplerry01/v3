namespace Whycespace.ProjectionRuntime.Projections.Core.Economics.Models;

public sealed record VaultBalanceModel(
    string VaultId,
    decimal CurrentBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    int TransactionCount,
    DateTime LastUpdated,
    string? VaultStatus = null,
    string? BalanceSummary = null
)
{
    public static VaultBalanceModel Initial(string vaultId, string? status = null) =>
        new(vaultId, 0m, 0m, 0m, 0, DateTime.UtcNow, status);

    public VaultBalanceModel ApplyCredit(decimal amount, DateTime timestamp) =>
        this with
        {
            CurrentBalance = CurrentBalance + amount,
            TotalCredits = TotalCredits + amount,
            TransactionCount = TransactionCount + 1,
            LastUpdated = timestamp,
            BalanceSummary = $"Credit {amount:F2} applied"
        };

    public VaultBalanceModel ApplyDebit(decimal amount, DateTime timestamp) =>
        this with
        {
            CurrentBalance = CurrentBalance - amount,
            TotalDebits = TotalDebits + amount,
            TransactionCount = TransactionCount + 1,
            LastUpdated = timestamp,
            BalanceSummary = $"Debit {amount:F2} applied"
        };

    public VaultBalanceModel WithStatus(string status, DateTime timestamp) =>
        this with { VaultStatus = status, LastUpdated = timestamp };
}
