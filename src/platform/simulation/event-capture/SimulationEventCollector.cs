namespace Whycespace.Platform.Simulation.EventCapture;

using System.Collections.Concurrent;

/// <summary>
/// Captures events produced during simulation without publishing them to the event fabric.
/// Events are stored per simulation ID for inspection and reporting.
/// Ensures no real state mutation occurs during simulation.
/// </summary>
public sealed class SimulationEventCollector
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<CapturedSimulationEvent>> _events = new();

    /// <summary>
    /// Captures an event produced during simulation. The event is stored but never published.
    /// </summary>
    public void Capture(Guid simulationId, string eventType, object payload, string wouldPublishToTopic)
    {
        var bag = _events.GetOrAdd(simulationId, _ => new ConcurrentBag<CapturedSimulationEvent>());
        bag.Add(new CapturedSimulationEvent(
            EventId: Guid.NewGuid(),
            SimulationId: simulationId,
            EventType: eventType,
            Payload: payload,
            WouldPublishToTopic: wouldPublishToTopic,
            CapturedAt: DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Returns all captured events for a given simulation run.
    /// </summary>
    public IReadOnlyList<CapturedSimulationEvent> GetEvents(Guid simulationId)
    {
        if (_events.TryGetValue(simulationId, out var bag))
            return bag.ToArray();
        return Array.Empty<CapturedSimulationEvent>();
    }

    /// <summary>
    /// Returns all captured events across all simulations.
    /// </summary>
    public IReadOnlyList<CapturedSimulationEvent> GetAllEvents()
    {
        return _events.Values.SelectMany(b => b).ToArray();
    }

    /// <summary>
    /// Returns the total count of captured events.
    /// </summary>
    public int TotalCaptured => _events.Values.Sum(b => b.Count);

    /// <summary>
    /// Clears all captured events for a specific simulation.
    /// </summary>
    public void Clear(Guid simulationId) => _events.TryRemove(simulationId, out _);

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void ClearAll() => _events.Clear();
}

public sealed record CapturedSimulationEvent(
    Guid EventId,
    Guid SimulationId,
    string EventType,
    object Payload,
    string WouldPublishToTopic,
    DateTimeOffset CapturedAt);
