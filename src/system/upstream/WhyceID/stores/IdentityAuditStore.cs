namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityAuditStore
{
    private readonly ConcurrentDictionary<Guid, IdentityAuditEvent> _events = new();

    public void Register(IdentityAuditEvent auditEvent)
    {
        _events[auditEvent.EventId] = auditEvent;
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetByIdentity(Guid identityId)
    {
        return _events.Values
            .Where(e => e.IdentityId == identityId)
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetAll()
    {
        return _events.Values
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }
}
