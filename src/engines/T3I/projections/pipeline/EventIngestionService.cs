
using Microsoft.Extensions.Logging;
using Whycespace.Shared.Envelopes;
using Whycespace.Engines.T3I.Projections.Registry;
using Whycespace.Contracts.Events;

namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Full event-driven intelligence pipeline.
///
/// Flow:
/// 1. Receive event (EventEnvelope)
/// 2. Apply to ProjectionStore via matching projections
/// 3. Build IntelligenceContext (delegated to registered pipeline steps)
/// 4. Execute IntelligenceOrchestrator (runs matching engines)
/// 5. Produce IngestionResult
///
/// This service does not publish events or mutate domain state.
/// </summary>
public sealed class EventIngestionService
{
    private readonly AtlasProjectionRegistry _projectionRegistry;
    private readonly IntelligenceOrchestrator _orchestrator;
    private readonly ILogger<EventIngestionService> _logger;

    public EventIngestionService(
        AtlasProjectionRegistry projectionRegistry,
        IntelligenceOrchestrator orchestrator,
        ILogger<EventIngestionService> logger)
    {
        _projectionRegistry = projectionRegistry;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestAsync(EventEnvelope envelope)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Ingesting event {EventId} of type {EventType} with correlation {CorrelationId}",
            envelope.EventId, envelope.EventType, correlationId);

        // Step 1–2: Apply event to matching projections
        var projectionApplied = await ApplyToProjectionsAsync(envelope);

        // Step 3–4: Build context and execute engines via orchestrator
        var engineResults = _orchestrator.Execute(envelope, correlationId);

        // Step 5: Produce result
        var result = new IngestionResult(
            correlationId,
            envelope.EventId,
            envelope.EventType,
            projectionApplied,
            engineResults,
            DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Ingestion complete for {EventId}: ProjectionApplied={ProjectionApplied}, EngineResults={EngineCount}, AllSucceeded={AllSucceeded}",
            envelope.EventId, result.ProjectionApplied, result.EngineResults.Count, result.AllEnginesSucceeded);

        return result;
    }

    private async Task<bool> ApplyToProjectionsAsync(EventEnvelope envelope)
    {
        var projections = _projectionRegistry.GetProjectionsForEventType(envelope.EventType);

        if (projections.Count == 0)
        {
            _logger.LogDebug("No projections registered for event type {EventType}", envelope.EventType);
            return false;
        }

        foreach (var projection in projections)
        {
            _logger.LogDebug("Applying event {EventId} to projection {ProjectionName}",
                envelope.EventId, projection.Name);

            await projection.HandleAsync(envelope);
        }

        return true;
    }
}
