namespace Whycespace.Systems.Midstream.WSS.Registry;

public sealed record WorkflowRegistryRecord(
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    WorkflowType WorkflowType,
    string DefinitionHash,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    WorkflowRegistryRecordStatus Status
);

public enum WorkflowType
{
    Definition = 0,
    Template = 1,
    Graph = 2
}

public enum WorkflowRegistryRecordStatus
{
    Active = 0,
    Deprecated = 1,
    Archived = 2
}
