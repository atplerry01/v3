namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityRoleEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _store;

    public IdentityRoleEngine(
        IdentityRegistry registry,
        IdentityRoleStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void AssignRole(Guid identityId, string role)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        _store.Assign(identityId, role);
    }

    public IReadOnlyCollection<string> GetRoles(Guid identityId)
    {
        return _store.GetRoles(identityId);
    }

    public bool HasRole(Guid identityId, string role)
    {
        return _store.HasRole(identityId, role);
    }
}
