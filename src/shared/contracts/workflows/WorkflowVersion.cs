namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowVersion(
    string WorkflowName,
    int Major,
    int Minor,
    bool IsActive = true
)
{
    public bool IsCompatibleWith(WorkflowVersion other)
        => WorkflowName == other.WorkflowName && Major == other.Major;

    public override string ToString() => $"{WorkflowName}:v{Major}.{Minor}";
}
