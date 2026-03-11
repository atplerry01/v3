namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;

public sealed class IdentityRoleStore
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _roles = new();

    public void Assign(Guid identityId, string role)
    {
        var set = _roles.GetOrAdd(identityId, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(role);
        }
    }

    public IReadOnlyCollection<string> GetRoles(Guid identityId)
    {
        if (_roles.TryGetValue(identityId, out var roles))
        {
            lock (roles)
            {
                return roles.ToList();
            }
        }

        return Array.Empty<string>();
    }

    public bool HasRole(Guid identityId, string role)
    {
        if (_roles.TryGetValue(identityId, out var roles))
        {
            lock (roles)
            {
                return roles.Contains(role);
            }
        }

        return false;
    }
}
