namespace Whycespace.Engines.T3I.Projections.Models;

public sealed record VaultCashflowModel(
    Guid SpvId,
    decimal TotalInflows,
    decimal TotalOutflows,
    decimal NetCashflow,
    int InflowCount,
    int OutflowCount,
    DateTimeOffset LastUpdatedAt);
