namespace Whycespace.Systems.Midstream.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;

public sealed record WorkflowTemplate(
    string TemplateId,
    string Name,
    int Version,
    string Description,
    IReadOnlyList<WorkflowTemplateStep> Steps,
    WorkflowGraph Graph
);
