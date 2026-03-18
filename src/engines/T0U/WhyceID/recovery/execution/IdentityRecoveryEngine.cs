namespace Whycespace.Engines.T0U.WhyceID.Recovery.Execution;

using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityRecoveryEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRecoveryStore _store;

    public IdentityRecoveryEngine(
        IdentityRegistry registry,
        IdentityRecoveryStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityRecovery CreateRecovery(Guid identityId, string reason)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Recovery reason cannot be empty.");

        var recovery = new IdentityRecovery(
            Guid.NewGuid(),
            identityId,
            reason,
            "pending",
            DateTime.UtcNow,
            null
        );

        _store.Register(recovery);

        return recovery;
    }

    public void ApproveRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);
        var updated = recovery with { Status = "approved" };
        _store.Update(updated);
    }

    public void RejectRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);
        var updated = recovery with { Status = "rejected" };
        _store.Update(updated);
    }

    public void CompleteRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);
        var updated = recovery with
        {
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        };
        _store.Update(updated);
    }

    public IReadOnlyCollection<IdentityRecovery> GetRecoveries(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityRecovery> GetAllRecoveries()
    {
        return _store.GetAll();
    }
}
