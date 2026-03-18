namespace Whycespace.Engines.T3I.Forecasting.Economic.Models;

public sealed record TrackCapitalLifecycleCommand(
    Guid CapitalId,
    CapitalLifecycleStage PreviousStage,
    CapitalLifecycleStage NewStage,
    Guid ReferenceId,
    Guid TriggeredBy,
    DateTimeOffset TriggeredAt);
