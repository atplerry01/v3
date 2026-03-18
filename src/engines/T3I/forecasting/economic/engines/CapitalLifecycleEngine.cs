using Whycespace.Engines.T3I.Forecasting.Economic.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Forecasting.Economic.Engines;

public sealed class CapitalLifecycleEngine : IIntelligenceEngine<TrackCapitalLifecycleCommand, CapitalLifecycleResult>
{
    public string EngineName => "CapitalLifecycle";

    private static readonly IReadOnlyDictionary<CapitalLifecycleStage, CapitalLifecycleStage> ValidTransitions =
        new Dictionary<CapitalLifecycleStage, CapitalLifecycleStage>
        {
            [CapitalLifecycleStage.Commitment] = CapitalLifecycleStage.Contribution,
            [CapitalLifecycleStage.Contribution] = CapitalLifecycleStage.Reservation,
            [CapitalLifecycleStage.Reservation] = CapitalLifecycleStage.Allocation,
            [CapitalLifecycleStage.Allocation] = CapitalLifecycleStage.Utilization,
            [CapitalLifecycleStage.Utilization] = CapitalLifecycleStage.Distribution,
            [CapitalLifecycleStage.Distribution] = CapitalLifecycleStage.Closed,
        };

    public IntelligenceResult<CapitalLifecycleResult> Execute(IntelligenceContext<TrackCapitalLifecycleCommand> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var command = context.Input;

        var validationError = Validate(command);
        if (validationError is not null)
        {
            return IntelligenceResult<CapitalLifecycleResult>.Fail(validationError,
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

        if (!IsValidTransition(command.PreviousStage, command.NewStage))
        {
            return IntelligenceResult<CapitalLifecycleResult>.Fail(
                $"Invalid lifecycle transition: {command.PreviousStage} -> {command.NewStage}",
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

        var record = new CapitalLifecycleRecord(
            CapitalId: command.CapitalId,
            PreviousStage: command.PreviousStage,
            CurrentStage: command.NewStage,
            ReferenceId: command.ReferenceId,
            Timestamp: command.TriggeredAt);

        var result = CapitalLifecycleResult.Ok(record);
        return IntelligenceResult<CapitalLifecycleResult>.Ok(result,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    public static bool IsValidTransition(CapitalLifecycleStage from, CapitalLifecycleStage to)
    {
        return ValidTransitions.TryGetValue(from, out var allowed) && allowed == to;
    }

    private static string? Validate(TrackCapitalLifecycleCommand command)
    {
        if (command.CapitalId == Guid.Empty)
            return "CapitalId must not be empty";

        if (command.ReferenceId == Guid.Empty)
            return "ReferenceId must not be empty";

        if (command.TriggeredBy == Guid.Empty)
            return "TriggeredBy must not be empty";

        if (!Enum.IsDefined(command.PreviousStage))
            return $"Invalid previous stage: {command.PreviousStage}";

        if (!Enum.IsDefined(command.NewStage))
            return $"Invalid new stage: {command.NewStage}";

        if (command.PreviousStage == command.NewStage)
            return "PreviousStage and NewStage must be different";

        return null;
    }
}

public sealed record CapitalLifecycleResult(
    bool Success,
    CapitalLifecycleRecord? Record,
    string? Error)
{
    public static CapitalLifecycleResult Ok(CapitalLifecycleRecord record) =>
        new(Success: true, Record: record, Error: null);

    public static CapitalLifecycleResult Fail(string error) =>
        new(Success: false, Record: null, Error: error);
}
