namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowRegistryEntry(
    string WorkflowId,
    string Name,
    int Version,
    WorkflowRegistryStatus Status,
    DateTimeOffset RegisteredAt
);

public enum WorkflowRegistryStatus
{
    Active = 0,
    Inactive = 1
}
