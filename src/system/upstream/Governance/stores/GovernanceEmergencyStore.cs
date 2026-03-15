namespace Whycespace.System.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.Governance.Models;

public sealed class GovernanceEmergencyStore
{
    private readonly ConcurrentDictionary<string, GovernanceEmergency> _emergencies = new();

    public void Add(GovernanceEmergency emergency)
    {
        if (!_emergencies.TryAdd(emergency.EmergencyId, emergency))
            throw new InvalidOperationException($"Emergency already exists: {emergency.EmergencyId}");
    }

    public GovernanceEmergency? Get(string emergencyId)
    {
        _emergencies.TryGetValue(emergencyId, out var emergency);
        return emergency;
    }

    public void Update(GovernanceEmergency emergency)
    {
        if (!_emergencies.ContainsKey(emergency.EmergencyId))
            throw new KeyNotFoundException($"Emergency not found: {emergency.EmergencyId}");

        _emergencies[emergency.EmergencyId] = emergency;
    }

    public bool Exists(string emergencyId)
    {
        return _emergencies.ContainsKey(emergencyId);
    }

    public IReadOnlyList<GovernanceEmergency> ListAll()
    {
        return _emergencies.Values.ToList().AsReadOnly();
    }
}
