namespace Whycespace.ArchitectureGuardrails.Rules;

public static class ArchitectureRules
{
    public const string StatelessEngines =
        "Engines must be stateless.";

    public const string NoEngineToEngineCalls =
        "Engines must not call other engines directly.";

    public const string WorkflowOnlyCommunication =
        "Engines only communicate through workflows.";

    public const string StateMutationsEmitEvents =
        "All state mutations must emit events.";

    public const string EventSourcingRequired =
        "Event sourcing is mandatory for all state changes.";

    public const string ProjectionsReadOnly =
        "Projections must not mutate state.";

    public const string DecisionEnginesReadProjections =
        "Decision engines must read projections only.";

    public const string DispatcherOnlyEntrypoint =
        "Runtime dispatcher is the only execution entrypoint.";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        StatelessEngines,
        NoEngineToEngineCalls,
        WorkflowOnlyCommunication,
        StateMutationsEmitEvents,
        EventSourcingRequired,
        ProjectionsReadOnly,
        DecisionEnginesReadProjections,
        DispatcherOnlyEntrypoint
    };

    public static IReadOnlyList<string> Names { get; } = new[]
    {
        nameof(StatelessEngines),
        nameof(NoEngineToEngineCalls),
        nameof(WorkflowOnlyCommunication),
        nameof(StateMutationsEmitEvents),
        nameof(EventSourcingRequired),
        nameof(ProjectionsReadOnly),
        nameof(DecisionEnginesReadProjections),
        nameof(DispatcherOnlyEntrypoint)
    };
}
