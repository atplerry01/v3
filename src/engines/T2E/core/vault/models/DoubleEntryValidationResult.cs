namespace Whycespace.Engines.T2E.Core.Vault.Models;

public sealed record DoubleEntryValidationResult(
    Guid TransactionId,
    bool IsBalanced,
    decimal TotalDebits,
    decimal TotalCredits,
    string ValidationStatus,
    string ValidationReason,
    DateTime EvaluatedAt);
