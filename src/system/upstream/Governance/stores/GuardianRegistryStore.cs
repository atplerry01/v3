namespace Whycespace.System.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.Governance.Models;

public sealed class GuardianRegistryStore
{
    private readonly ConcurrentDictionary<string, Guardian> _guardians = new();

    public void Register(Guardian guardian)
    {
        if (!_guardians.TryAdd(guardian.GuardianId, guardian))
            throw new InvalidOperationException($"Guardian already registered: {guardian.GuardianId}");
    }

    public Guardian? GetGuardian(string guardianId)
    {
        _guardians.TryGetValue(guardianId, out var guardian);
        return guardian;
    }

    public IReadOnlyList<Guardian> ListGuardians()
    {
        return _guardians.Values.ToList();
    }

    public void ActivateGuardian(string guardianId)
    {
        if (!_guardians.TryGetValue(guardianId, out var guardian))
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        if (guardian.Status == GuardianStatus.Active)
            throw new InvalidOperationException($"Guardian already active: {guardianId}");

        _guardians[guardianId] = guardian with
        {
            Status = GuardianStatus.Active,
            ActivatedAt = DateTime.UtcNow
        };
    }

    public void DeactivateGuardian(string guardianId)
    {
        if (!_guardians.TryGetValue(guardianId, out var guardian))
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        if (guardian.Status == GuardianStatus.Inactive)
            throw new InvalidOperationException($"Guardian already inactive: {guardianId}");

        _guardians[guardianId] = guardian with { Status = GuardianStatus.Inactive };
    }

    public bool Exists(string guardianId)
    {
        return _guardians.ContainsKey(guardianId);
    }
}
