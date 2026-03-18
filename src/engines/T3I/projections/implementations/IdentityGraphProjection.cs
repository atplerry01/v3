
using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Shared.Envelopes;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.Contracts.Events;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Implementations;

public sealed class IdentityGraphProjection : IProjection
{
    private readonly AtlasProjectionStore<IdentityGraphModel> _store;

    public IdentityGraphProjection(AtlasProjectionStore<IdentityGraphModel> store)
    {
        _store = store;
    }

    public string Name => "AtlasIdentityGraph";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "whyce.identity.registered",
        "whyce.identity.activated",
        "whyce.identity.suspended",
        "whyce.identity.role-assigned"
    ];

    public Task HandleAsync(EventEnvelope envelope)
    {
        if (_store.HasProcessed(envelope.EventId))
            return Task.CompletedTask;

        switch (envelope.EventType)
        {
            case "whyce.identity.registered":
                ApplyRegistered(envelope);
                break;

            case "whyce.identity.activated":
                ApplyStatusChange(envelope, "Active");
                break;

            case "whyce.identity.suspended":
                ApplyStatusChange(envelope, "Suspended");
                break;

            case "whyce.identity.role-assigned":
                ApplyRoleAssigned(envelope);
                break;
        }

        _store.MarkProcessed(envelope.EventId);
        return Task.CompletedTask;
    }

    public AtlasProjectionStore<IdentityGraphModel> Store => _store;

    private void ApplyRegistered(EventEnvelope envelope)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var identityId = ExtractGuid(data, "IdentityId");
        if (identityId == Guid.Empty) return;

        data.TryGetValue("DisplayName", out var displayNameObj);
        data.TryGetValue("Email", out var emailObj);
        var displayName = displayNameObj?.ToString() ?? "";
        var email = emailObj?.ToString() ?? "";

        _store.Upsert(identityId, new IdentityGraphModel(
            identityId,
            displayName,
            email,
            Array.Empty<string>(),
            "Registered",
            envelope.Timestamp.Value));
    }

    private void ApplyStatusChange(EventEnvelope envelope, string status)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var identityId = ExtractGuid(data, "IdentityId");
        if (identityId == Guid.Empty) return;

        var current = _store.Get(identityId);
        if (current is null) return;

        _store.Upsert(identityId, current with
        {
            Status = status,
            LastUpdatedAt = envelope.Timestamp.Value
        });
    }

    private void ApplyRoleAssigned(EventEnvelope envelope)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var identityId = ExtractGuid(data, "IdentityId");
        if (identityId == Guid.Empty) return;

        data.TryGetValue("Role", out var roleObj);
        var role = roleObj?.ToString();
        if (string.IsNullOrEmpty(role)) return;

        var current = _store.Get(identityId);
        if (current is null) return;

        var roles = current.Roles.Contains(role)
            ? current.Roles
            : current.Roles.Append(role).ToList();

        _store.Upsert(identityId, current with
        {
            Roles = roles,
            LastUpdatedAt = envelope.Timestamp.Value
        });
    }

    private static Guid ExtractGuid(IDictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value is Guid guid)
            return guid;

        if (value is string s && Guid.TryParse(s, out var parsed))
            return parsed;

        return Guid.Empty;
    }
}
