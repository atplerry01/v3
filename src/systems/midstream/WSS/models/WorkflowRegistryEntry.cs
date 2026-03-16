namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record WorkflowRegistryEntry(
    string WorkflowId,
    string Name,
    string Version,
    WorkflowRegistryStatus Status,
    DateTimeOffset RegisteredAt
);

public enum WorkflowRegistryStatus
{
    Active = 0,
    Inactive = 1
}
