namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowTemplate(
    string TemplateId,
    string WorkflowDefinitionId,
    IReadOnlyDictionary<string, string> Parameters,
    DateTimeOffset CreatedAt
);
