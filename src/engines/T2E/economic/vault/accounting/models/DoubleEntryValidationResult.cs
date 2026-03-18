namespace Whycespace.Engines.T2E.Economic.Vault.Accounting.Models;

public sealed record DoubleEntryValidationResult(
    Guid TransactionId,
    bool IsBalanced,
    decimal TotalDebits,
    decimal TotalCredits,
    string ValidationStatus,
    string ValidationReason,
    DateTime EvaluatedAt);
