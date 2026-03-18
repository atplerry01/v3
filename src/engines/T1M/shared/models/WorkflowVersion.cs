namespace Whycespace.Engines.T1M.Shared;

public sealed record WorkflowVersion(
    string WorkflowName,
    string Version,
    string BaseVersion,
    string CompatibilityLevel,
    string ChangeDescription,
    DateTimeOffset CreatedAt);
