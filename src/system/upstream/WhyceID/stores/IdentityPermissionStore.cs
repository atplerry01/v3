namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;

public sealed class IdentityPermissionStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _permissions = new();

    public void Assign(string role, string permission)
    {
        var set = _permissions.GetOrAdd(role, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(permission);
        }
    }

    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        if (_permissions.TryGetValue(role, out var set))
        {
            lock (set)
            {
                return set.ToList();
            }
        }

        return Array.Empty<string>();
    }

    public bool HasPermission(string role, string permission)
    {
        if (_permissions.TryGetValue(role, out var set))
        {
            lock (set)
            {
                return set.Contains(permission);
            }
        }

        return false;
    }
}
