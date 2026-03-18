namespace Whycespace.Platform.Simulation.Trace;

using System.Collections.Concurrent;

/// <summary>
/// Collects execution traces during simulation runs.
/// Captures step-by-step execution flow for debugging, reporting, and audit purposes.
/// Thread-safe for concurrent simulation execution.
/// </summary>
public sealed class SimulationTraceCollector
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<SimulationTraceEntry>> _traces = new();

    /// <summary>
    /// Records a trace entry for a simulation step.
    /// </summary>
    public void Record(Guid simulationId, string stepName, string description)
    {
        var bag = _traces.GetOrAdd(simulationId, _ => new ConcurrentBag<SimulationTraceEntry>());
        bag.Add(new SimulationTraceEntry(
            SimulationId: simulationId,
            StepName: stepName,
            Description: description,
            Timestamp: DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Returns all trace entries for a given simulation, ordered by timestamp.
    /// </summary>
    public IReadOnlyList<SimulationTraceEntry> GetTraces(Guid simulationId)
    {
        if (_traces.TryGetValue(simulationId, out var bag))
            return bag.OrderBy(t => t.Timestamp).ToArray();
        return Array.Empty<SimulationTraceEntry>();
    }

    /// <summary>
    /// Returns all trace entries across all simulations.
    /// </summary>
    public IReadOnlyList<SimulationTraceEntry> GetAllTraces()
    {
        return _traces.Values
            .SelectMany(b => b)
            .OrderBy(t => t.Timestamp)
            .ToArray();
    }

    /// <summary>
    /// Returns the total count of trace entries.
    /// </summary>
    public int TotalTraces => _traces.Values.Sum(b => b.Count);

    /// <summary>
    /// Clears traces for a specific simulation.
    /// </summary>
    public void Clear(Guid simulationId) => _traces.TryRemove(simulationId, out _);

    /// <summary>
    /// Clears all traces.
    /// </summary>
    public void ClearAll() => _traces.Clear();
}

public sealed record SimulationTraceEntry(
    Guid SimulationId,
    string StepName,
    string Description,
    DateTimeOffset Timestamp);
