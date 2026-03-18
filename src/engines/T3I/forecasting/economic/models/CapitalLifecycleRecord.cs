namespace Whycespace.Engines.T3I.Forecasting.Economic.Models;

public sealed record CapitalLifecycleRecord(
    Guid CapitalId,
    CapitalLifecycleStage PreviousStage,
    CapitalLifecycleStage CurrentStage,
    Guid ReferenceId,
    DateTimeOffset Timestamp);
