namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityPermissionEngine
{
    private readonly IdentityPermissionStore _store;

    public IdentityPermissionEngine(
        IdentityPermissionStore store)
    {
        _store = store;
    }

    public void AssignPermission(string role, string permission)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be empty.");
        }

        if (!permission.Contains(':'))
        {
            throw new ArgumentException(
                "Permission must follow format resource:action");
        }

        _store.Assign(role, permission);
    }

    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        return _store.GetPermissions(role);
    }

    public bool HasPermission(string role, string permission)
    {
        return _store.HasPermission(role, permission);
    }
}
