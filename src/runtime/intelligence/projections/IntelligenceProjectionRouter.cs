namespace Whycespace.IntelligenceRuntime.Projections;

using Whycespace.EventFabric.Models;
using Whycespace.IntelligenceRuntime.Models;
using Whycespace.IntelligenceRuntime.Orchestrator;

public sealed class IntelligenceProjectionRouter
{
    private readonly Dictionary<string, List<IntelligenceProjectionBinding>> _bindings = new(StringComparer.Ordinal);
    private readonly IntelligenceOrchestrator _orchestrator;

    public IntelligenceProjectionRouter(IntelligenceOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public void Bind(string eventType, IntelligenceCapability capability, string engineId,
        Func<EventEnvelope, IReadOnlyDictionary<string, object>> parameterExtractor)
    {
        if (!_bindings.TryGetValue(eventType, out var list))
        {
            list = new List<IntelligenceProjectionBinding>();
            _bindings[eventType] = list;
        }

        list.Add(new IntelligenceProjectionBinding(capability, engineId, parameterExtractor));
    }

    public async Task<IReadOnlyList<IntelligenceResult>> ProcessEventAsync(EventEnvelope envelope)
    {
        if (!_bindings.TryGetValue(envelope.EventType, out var bindings))
            return [];

        var results = new List<IntelligenceResult>(bindings.Count);

        foreach (var binding in bindings)
        {
            var parameters = binding.ParameterExtractor(envelope);
            var request = IntelligenceRequest.Create(
                binding.Capability,
                binding.EngineId,
                parameters,
                envelope.EventId.ToString());

            var result = await _orchestrator.ExecuteAsync(request);
            results.Add(result);
        }

        return results;
    }

    public IReadOnlyCollection<string> GetBoundEventTypes() => _bindings.Keys;

    public int BindingCount => _bindings.Values.Sum(b => b.Count);
}

public sealed record IntelligenceProjectionBinding(
    IntelligenceCapability Capability,
    string EngineId,
    Func<EventEnvelope, IReadOnlyDictionary<string, object>> ParameterExtractor
);
