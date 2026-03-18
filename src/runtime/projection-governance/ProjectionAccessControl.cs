namespace Whycespace.Runtime.ProjectionGovernance;

public sealed class ProjectionAccessControl
{
    private readonly Dictionary<string, HashSet<string>> _accessRules = new();

    public void GrantAccess(string projectionName, string roleOrIdentity)
    {
        if (!_accessRules.ContainsKey(projectionName))
            _accessRules[projectionName] = new HashSet<string>();

        _accessRules[projectionName].Add(roleOrIdentity);
    }

    public void RevokeAccess(string projectionName, string roleOrIdentity)
    {
        if (_accessRules.TryGetValue(projectionName, out var roles))
            roles.Remove(roleOrIdentity);
    }

    public bool HasAccess(string projectionName, string roleOrIdentity)
    {
        // If no rules configured, default to allowed
        if (!_accessRules.TryGetValue(projectionName, out var roles))
            return true;

        return roles.Contains(roleOrIdentity);
    }

    public IReadOnlyCollection<string> GetAuthorizedRoles(string projectionName)
    {
        if (_accessRules.TryGetValue(projectionName, out var roles))
            return roles.ToList().AsReadOnly();

        return Array.Empty<string>();
    }
}
