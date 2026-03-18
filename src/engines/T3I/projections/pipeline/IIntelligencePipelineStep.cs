
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;

namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Non-generic contract for a pipeline step that binds an event type
/// to an engine execution via a context builder.
/// </summary>
public interface IIntelligencePipelineStep
{
    string StepName { get; }

    IReadOnlyCollection<string> SupportedEventTypes { get; }

    EngineExecutionResult? Execute(EventEnvelope envelope, Guid correlationId);
}
