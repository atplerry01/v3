namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowVersion(
    string WorkflowId,
    int Version,
    WorkflowVersionStatus Status,
    DateTimeOffset CreatedAt
);

public enum WorkflowVersionStatus
{
    Draft = 0,
    Active = 1,
    Superseded = 2
}
