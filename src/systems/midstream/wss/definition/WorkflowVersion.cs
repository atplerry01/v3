namespace Whycespace.Systems.Midstream.WSS.Definition;

public sealed record WorkflowVersion(
    string WorkflowId,
    string Version,
    WorkflowVersionStatus Status,
    DateTimeOffset CreatedAt
);

public enum WorkflowVersionStatus
{
    Draft = 0,
    Active = 1,
    Superseded = 2
}
