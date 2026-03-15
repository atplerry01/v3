namespace Whycespace.Engines.T1M.WSS.Versioning;

public sealed record WorkflowVersionResult(
    bool Success,
    string WorkflowId,
    string WorkflowName,
    string NewVersion,
    string BaseVersion,
    CompatibilityLevel CompatibilityLevel,
    string ChangeDescription,
    DateTimeOffset CreatedAt,
    string Message);

public enum CompatibilityLevel
{
    Compatible,
    BackwardCompatible,
    Breaking
}
