namespace Whycespace.Systems.Midstream.WSS.Registry;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IWorkflowRegistryStore _store;

    public WorkflowRegistry(IWorkflowRegistryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public WorkflowRegistryRecord RegisterWorkflowDefinition(WorkflowDefinition definition, string createdBy)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        if (string.IsNullOrWhiteSpace(definition.WorkflowId))
            throw new InvalidOperationException("WorkflowDefinition must have a valid WorkflowId.");

        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new InvalidOperationException("WorkflowDefinition must have a valid Name.");

        if (_store.ExistsById(definition.WorkflowId))
            throw new InvalidOperationException($"Workflow already registered: {definition.WorkflowId}");

        if (_store.ExistsByNameAndVersion(definition.Name, definition.Version))
            throw new InvalidOperationException($"Workflow version already exists: {definition.Name} v{definition.Version}");

        var hash = ComputeHash(definition);

        var record = new WorkflowRegistryRecord(
            WorkflowId: definition.WorkflowId,
            WorkflowName: definition.Name,
            WorkflowVersion: definition.Version,
            WorkflowType: WorkflowType.Definition,
            DefinitionHash: hash,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: createdBy,
            Status: WorkflowRegistryRecordStatus.Active
        );

        _store.Save(record);
        return record;
    }

    public WorkflowRegistryRecord RegisterWorkflowTemplate(WorkflowTemplate template, string createdBy)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        if (string.IsNullOrWhiteSpace(template.TemplateId))
            throw new InvalidOperationException("WorkflowTemplate must have a valid TemplateId.");

        if (string.IsNullOrWhiteSpace(template.Name))
            throw new InvalidOperationException("WorkflowTemplate must have a valid Name.");

        if (_store.ExistsById(template.TemplateId))
            throw new InvalidOperationException($"Workflow already registered: {template.TemplateId}");

        var hash = ComputeHash(template);

        var record = new WorkflowRegistryRecord(
            WorkflowId: template.TemplateId,
            WorkflowName: template.Name,
            WorkflowVersion: template.Version.ToString(),
            WorkflowType: WorkflowType.Template,
            DefinitionHash: hash,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: createdBy,
            Status: WorkflowRegistryRecordStatus.Active
        );

        _store.Save(record);
        return record;
    }

    public WorkflowRegistryRecord RegisterWorkflowGraph(WorkflowGraph graph, string workflowName, string version, string createdBy)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        if (string.IsNullOrWhiteSpace(graph.WorkflowId))
            throw new InvalidOperationException("WorkflowGraph must have a valid WorkflowId.");

        if (_store.ExistsById(graph.WorkflowId))
            throw new InvalidOperationException($"Workflow already registered: {graph.WorkflowId}");

        var hash = ComputeHash(graph);

        var record = new WorkflowRegistryRecord(
            WorkflowId: graph.WorkflowId,
            WorkflowName: workflowName,
            WorkflowVersion: version,
            WorkflowType: WorkflowType.Graph,
            DefinitionHash: hash,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: createdBy,
            Status: WorkflowRegistryRecordStatus.Active
        );

        _store.Save(record);
        return record;
    }

    public WorkflowRegistryRecord? ResolveWorkflow(string workflowName, string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);

        if (version is not null)
            return _store.GetByNameAndVersion(workflowName, version);

        return _store.GetByName(workflowName);
    }

    public WorkflowRegistryRecord? GetWorkflow(string workflowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        return _store.GetById(workflowId);
    }

    public IReadOnlyList<WorkflowRegistryRecord> ListWorkflows()
    {
        return _store.GetAll();
    }

    public IReadOnlyList<WorkflowRegistryRecord> ListWorkflowsByType(WorkflowType type)
    {
        if (!Enum.IsDefined(type))
            throw new InvalidOperationException($"Invalid workflow type: {type}");

        return _store.GetByType(type);
    }

    public void UpdateStatus(string workflowId, WorkflowRegistryRecordStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);

        if (!Enum.IsDefined(status))
            throw new InvalidOperationException($"Invalid status: {status}");

        var existing = _store.GetById(workflowId)
            ?? throw new KeyNotFoundException($"Workflow not found: {workflowId}");

        _store.Update(existing with { Status = status });
    }

    private static string ComputeHash(object value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
