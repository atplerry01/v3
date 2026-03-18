namespace Whycespace.Systems.Midstream.WSS.Definition;

using Whycespace.Contracts.Workflows;

public sealed record WorkflowDefinition(
    string WorkflowId,
    string Name,
    string Description,
    string Version,
    IReadOnlyList<WorkflowStep> Steps,
    DateTimeOffset CreatedAt
);
