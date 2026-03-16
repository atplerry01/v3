namespace Whycespace.Systems.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

public sealed class IdentityRegistryStore : IIdentityRegistry
{
    private readonly ConcurrentDictionary<Guid, IdentityRecord> _identities = new();
    private readonly ConcurrentDictionary<string, Guid> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _phoneIndex = new();

    public void RegisterIdentity(IdentityRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!_identities.TryAdd(record.IdentityId, record))
        {
            throw new InvalidOperationException(
                $"Identity already registered: {record.IdentityId}");
        }

        if (!string.IsNullOrWhiteSpace(record.PrimaryEmail))
        {
            _emailIndex.TryAdd(record.PrimaryEmail, record.IdentityId);
        }

        if (!string.IsNullOrWhiteSpace(record.PrimaryPhone))
        {
            _phoneIndex.TryAdd(record.PrimaryPhone, record.IdentityId);
        }
    }

    public IdentityRecord GetIdentity(Guid identityId)
    {
        if (!_identities.TryGetValue(identityId, out var record))
        {
            throw new KeyNotFoundException(
                $"Identity not found: {identityId}");
        }

        return record;
    }

    public IdentityRecord? GetIdentityByEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (_emailIndex.TryGetValue(email, out var id) &&
            _identities.TryGetValue(id, out var record))
        {
            return record;
        }

        return null;
    }

    public IdentityRecord? GetIdentityByPhone(string phone)
    {
        ArgumentNullException.ThrowIfNull(phone);

        if (_phoneIndex.TryGetValue(phone, out var id) &&
            _identities.TryGetValue(id, out var record))
        {
            return record;
        }

        return null;
    }

    public void UpdateIdentityStatus(Guid identityId, IdentityStatus status)
    {
        if (!_identities.TryGetValue(identityId, out var existing))
        {
            throw new KeyNotFoundException(
                $"Identity not found: {identityId}");
        }

        var updated = existing.WithStatus(status);
        _identities[identityId] = updated;
    }

    public IReadOnlyList<IdentityRecord> ListIdentities(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be >= 1.");

        return _identities.Values
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();
    }

    public int Count => _identities.Count;
}
