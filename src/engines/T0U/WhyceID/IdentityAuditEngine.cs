namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityAuditEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAuditStore _store;

    public IdentityAuditEngine(
        IdentityRegistry registry,
        IdentityAuditStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityAuditEvent RecordEvent(
        Guid identityId,
        string eventType,
        string description)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty.");

        var auditEvent = new IdentityAuditEvent(
            Guid.NewGuid(),
            identityId,
            eventType,
            description ?? "",
            DateTime.UtcNow
        );

        _store.Register(auditEvent);

        return auditEvent;
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetIdentityAudit(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetAllAuditEvents()
    {
        return _store.GetAll();
    }
}
