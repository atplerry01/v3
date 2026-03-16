namespace Whycespace.Engines.T3I.Capital;

public sealed record CapitalLifecycleRecord(
    Guid CapitalId,
    CapitalLifecycleStage PreviousStage,
    CapitalLifecycleStage CurrentStage,
    Guid ReferenceId,
    DateTimeOffset Timestamp);
