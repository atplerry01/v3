namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.System.WhyceID.Stores;

public sealed class IdentityAccessScopeEngine
{
    private readonly IdentityAccessScopeStore _store;

    public IdentityAccessScopeEngine(
        IdentityAccessScopeStore store)
    {
        _store = store;
    }

    public void AssignScope(string role, string scope)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope cannot be empty.");
        }

        if (!scope.Contains(':'))
        {
            throw new ArgumentException(
                "Scope must follow format resource:value");
        }

        _store.Assign(role, scope);
    }

    public IReadOnlyCollection<string> GetScopes(string role)
    {
        return _store.GetScopes(role);
    }

    public bool HasScope(string role, string scope)
    {
        return _store.HasScope(role, scope);
    }
}
