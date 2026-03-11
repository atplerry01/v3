namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;

public sealed class IdentityAccessScopeStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _scopes = new();

    public void Assign(string role, string scope)
    {
        var set = _scopes.GetOrAdd(role, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(scope);
        }
    }

    public IReadOnlyCollection<string> GetScopes(string role)
    {
        if (_scopes.TryGetValue(role, out var set))
        {
            lock (set)
            {
                return set.ToList();
            }
        }

        return Array.Empty<string>();
    }

    public bool HasScope(string role, string scope)
    {
        if (_scopes.TryGetValue(role, out var set))
        {
            lock (set)
            {
                return set.Contains(scope);
            }
        }

        return false;
    }
}
