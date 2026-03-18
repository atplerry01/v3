namespace Whycespace.Runtime.ProjectionGovernance;

public sealed class ProjectionPolicy
{
    private readonly HashSet<string> _allowedProjections = new();
    private readonly HashSet<string> _blockedProjections = new();

    public bool IsProjectionAllowed(string projectionName)
    {
        if (_blockedProjections.Contains(projectionName))
            return false;

        // If allowlist is configured, only allow listed projections
        if (_allowedProjections.Count > 0)
            return _allowedProjections.Contains(projectionName);

        return true;
    }

    public void Allow(string projectionName) => _allowedProjections.Add(projectionName);
    public void Block(string projectionName) => _blockedProjections.Add(projectionName);
    public void Unblock(string projectionName) => _blockedProjections.Remove(projectionName);
}
