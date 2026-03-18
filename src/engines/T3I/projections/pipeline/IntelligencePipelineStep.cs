
using Whycespace.Engines.T3I.Shared;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;

namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Generic pipeline step that binds an <see cref="IIntelligenceEngine{TInput, TOutput}"/>
/// to a context builder function. The context builder produces typed engine input
/// from the current projection state after an event has been applied.
/// </summary>
public sealed class IntelligencePipelineStep<TInput, TOutput> : IIntelligencePipelineStep
{
    private readonly IIntelligenceEngine<TInput, TOutput> _engine;
    private readonly Func<EventEnvelope, TInput?> _contextBuilder;
    private readonly IReadOnlyCollection<string> _eventTypes;

    public IntelligencePipelineStep(
        IIntelligenceEngine<TInput, TOutput> engine,
        IReadOnlyCollection<string> eventTypes,
        Func<EventEnvelope, TInput?> contextBuilder)
    {
        _engine = engine;
        _eventTypes = eventTypes;
        _contextBuilder = contextBuilder;
    }

    public string StepName => _engine.EngineName;

    public IReadOnlyCollection<string> SupportedEventTypes => _eventTypes;

    public EngineExecutionResult? Execute(EventEnvelope envelope, Guid correlationId)
    {
        var input = _contextBuilder(envelope);
        if (input is null)
            return null;

        var context = IntelligenceContext<TInput>.Create(correlationId, input);
        var result = _engine.Execute(context);

        return new EngineExecutionResult(
            result.Trace.EngineName,
            result.Success,
            result.Output,
            result.Error,
            result.Trace);
    }
}
