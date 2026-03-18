namespace Whycespace.Engines.T3I.Projections.Models;

public sealed record CapitalBalanceModel(
    Guid SpvId,
    decimal TotalContributions,
    decimal TotalDistributions,
    decimal TotalReserved,
    decimal NetBalance,
    int TransactionCount,
    DateTimeOffset LastUpdatedAt);
