namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityRecoveryStore
{
    private readonly ConcurrentDictionary<Guid, IdentityRecovery> _recoveries = new();

    public void Register(IdentityRecovery recovery)
    {
        _recoveries[recovery.RecoveryId] = recovery;
    }

    public IdentityRecovery Get(Guid recoveryId)
    {
        if (!_recoveries.TryGetValue(recoveryId, out var recovery))
            throw new KeyNotFoundException($"Recovery not found: {recoveryId}");

        return recovery;
    }

    public IReadOnlyCollection<IdentityRecovery> GetByIdentity(Guid identityId)
    {
        return _recoveries.Values
            .Where(r => r.IdentityId == identityId)
            .ToList();
    }

    public void Update(IdentityRecovery recovery)
    {
        _recoveries[recovery.RecoveryId] = recovery;
    }

    public IReadOnlyCollection<IdentityRecovery> GetAll()
    {
        return _recoveries.Values.ToList();
    }
}
