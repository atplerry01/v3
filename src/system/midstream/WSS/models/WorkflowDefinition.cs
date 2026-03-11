namespace Whycespace.System.Midstream.WSS.Models;

using Whycespace.Contracts.Workflows;

public sealed record WorkflowDefinition(
    string WorkflowId,
    string Name,
    string Description,
    int Version,
    IReadOnlyList<WorkflowStep> Steps,
    DateTimeOffset CreatedAt
);
