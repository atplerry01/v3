
using Microsoft.Extensions.Logging;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;

namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Orchestrates execution of registered intelligence pipeline steps
/// for a given event type. Each step binds an engine to a context builder.
/// </summary>
public sealed class IntelligenceOrchestrator
{
    private readonly List<IIntelligencePipelineStep> _steps = [];
    private readonly ILogger<IntelligenceOrchestrator> _logger;

    public IntelligenceOrchestrator(ILogger<IntelligenceOrchestrator> logger)
    {
        _logger = logger;
    }

    public void Register(IIntelligencePipelineStep step)
    {
        _steps.Add(step);
        _logger.LogDebug("Registered pipeline step {StepName} for event types: {EventTypes}",
            step.StepName, string.Join(", ", step.SupportedEventTypes));
    }

    public IReadOnlyList<EngineExecutionResult> Execute(EventEnvelope envelope, Guid correlationId)
    {
        var matchingSteps = _steps
            .Where(s => s.SupportedEventTypes.Contains(envelope.EventType))
            .ToList();

        if (matchingSteps.Count == 0)
        {
            _logger.LogDebug("No pipeline steps registered for event type {EventType}", envelope.EventType);
            return [];
        }

        var results = new List<EngineExecutionResult>();

        foreach (var step in matchingSteps)
        {
            _logger.LogDebug("Executing pipeline step {StepName} for correlation {CorrelationId}",
                step.StepName, correlationId);

            var result = step.Execute(envelope, correlationId);
            if (result is not null)
            {
                results.Add(result);

                _logger.LogDebug("Pipeline step {StepName} completed: Success={Success}",
                    step.StepName, result.Success);
            }
        }

        return results;
    }

    public IReadOnlyCollection<string> GetRegisteredEventTypes() =>
        _steps
            .SelectMany(s => s.SupportedEventTypes)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public int StepCount => _steps.Count;
}
