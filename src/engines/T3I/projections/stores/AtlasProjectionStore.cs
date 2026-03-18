using System.Collections.Concurrent;

namespace Whycespace.Engines.T3I.Projections.Stores;

public sealed class AtlasProjectionStore<TModel> where TModel : class
{
    private readonly ConcurrentDictionary<Guid, TModel> _state = new();
    private readonly ConcurrentDictionary<Guid, byte> _processedEvents = new();

    public bool HasProcessed(Guid eventId) => _processedEvents.ContainsKey(eventId);

    public void MarkProcessed(Guid eventId) => _processedEvents.TryAdd(eventId, 0);

    public void Upsert(Guid key, TModel model) => _state[key] = model;

    public TModel? Get(Guid key) => _state.GetValueOrDefault(key);

    public IReadOnlyCollection<TModel> GetAll() => _state.Values.ToList();

    public int Count => _state.Count;

    public void Clear()
    {
        _state.Clear();
        _processedEvents.Clear();
    }
}
