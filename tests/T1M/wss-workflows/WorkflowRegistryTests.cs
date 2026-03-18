namespace Whycespace.Tests.WssWorkflows;

using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Registry;
using Xunit;

public sealed class WorkflowRegistryTests
{
    private readonly WorkflowRegistry _registry;
    private readonly WorkflowRegistryStore _store;

    public WorkflowRegistryTests()
    {
        _store = new WorkflowRegistryStore();
        _registry = new WorkflowRegistry(_store);
    }

    [Fact]
    public void RegisterWorkflowDefinition_Succeeds()
    {
        var definition = CreateDefinition("wf-1", "TestWorkflow", "1.0.0");

        var record = _registry.RegisterWorkflowDefinition(definition, "test-user");

        Assert.Equal("wf-1", record.WorkflowId);
        Assert.Equal("TestWorkflow", record.WorkflowName);
        Assert.Equal("1.0.0", record.WorkflowVersion);
        Assert.Equal(WorkflowType.Definition, record.WorkflowType);
        Assert.Equal(WorkflowRegistryRecordStatus.Active, record.Status);
        Assert.Equal("test-user", record.CreatedBy);
        Assert.NotEmpty(record.DefinitionHash);
    }

    [Fact]
    public void RegisterWorkflowTemplate_Succeeds()
    {
        var template = new WorkflowTemplate(
            TemplateId: "tmpl-1",
            Name: "TestTemplate",
            Version: 1,
            Description: "A test template",
            Steps: Array.Empty<WorkflowTemplateStep>(),
            Graph: new WorkflowGraph("graph-tmpl-1", new Dictionary<string, IReadOnlyList<string>>())
        );

        var record = _registry.RegisterWorkflowTemplate(template, "test-user");

        Assert.Equal("tmpl-1", record.WorkflowId);
        Assert.Equal("TestTemplate", record.WorkflowName);
        Assert.Equal("1", record.WorkflowVersion);
        Assert.Equal(WorkflowType.Template, record.WorkflowType);
        Assert.Equal(WorkflowRegistryRecordStatus.Active, record.Status);
    }

    [Fact]
    public void RegisterWorkflowGraph_Succeeds()
    {
        var graph = new WorkflowGraph("graph-1", new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-1"] = new[] { "step-2" },
            ["step-2"] = Array.Empty<string>()
        });

        var record = _registry.RegisterWorkflowGraph(graph, "GraphWorkflow", "1.0.0", "test-user");

        Assert.Equal("graph-1", record.WorkflowId);
        Assert.Equal("GraphWorkflow", record.WorkflowName);
        Assert.Equal(WorkflowType.Graph, record.WorkflowType);
    }

    [Fact]
    public void ResolveWorkflow_ByName_ReturnsLatestActive()
    {
        var def1 = CreateDefinition("wf-v1", "MyWorkflow", "1.0.0");
        var def2 = CreateDefinition("wf-v2", "MyWorkflow", "2.0.0");

        _registry.RegisterWorkflowDefinition(def1, "test-user");
        _registry.RegisterWorkflowDefinition(def2, "test-user");

        var resolved = _registry.ResolveWorkflow("MyWorkflow");

        Assert.NotNull(resolved);
        Assert.Equal("2.0.0", resolved.WorkflowVersion);
    }

    [Fact]
    public void ResolveWorkflow_ByNameAndVersion_ReturnsExactVersion()
    {
        var def1 = CreateDefinition("wf-v1", "MyWorkflow", "1.0.0");
        var def2 = CreateDefinition("wf-v2", "MyWorkflow", "2.0.0");

        _registry.RegisterWorkflowDefinition(def1, "test-user");
        _registry.RegisterWorkflowDefinition(def2, "test-user");

        var resolved = _registry.ResolveWorkflow("MyWorkflow", "1.0.0");

        Assert.NotNull(resolved);
        Assert.Equal("wf-v1", resolved.WorkflowId);
    }

    [Fact]
    public void ResolveWorkflow_NotFound_ReturnsNull()
    {
        var result = _registry.ResolveWorkflow("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetWorkflow_ById_ReturnsRecord()
    {
        var definition = CreateDefinition("wf-1", "TestWorkflow", "1.0.0");
        _registry.RegisterWorkflowDefinition(definition, "test-user");

        var record = _registry.GetWorkflow("wf-1");

        Assert.NotNull(record);
        Assert.Equal("TestWorkflow", record.WorkflowName);
    }

    [Fact]
    public void ListWorkflows_ReturnsAll()
    {
        _registry.RegisterWorkflowDefinition(CreateDefinition("wf-1", "Workflow1", "1.0.0"), "user");
        _registry.RegisterWorkflowDefinition(CreateDefinition("wf-2", "Workflow2", "1.0.0"), "user");

        var all = _registry.ListWorkflows();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void ListWorkflowsByType_FiltersCorrectly()
    {
        _registry.RegisterWorkflowDefinition(CreateDefinition("wf-1", "Def1", "1.0.0"), "user");
        _registry.RegisterWorkflowGraph(
            new WorkflowGraph("graph-1", new Dictionary<string, IReadOnlyList<string>>()),
            "Graph1", "1.0.0", "user");

        var definitions = _registry.ListWorkflowsByType(WorkflowType.Definition);
        var graphs = _registry.ListWorkflowsByType(WorkflowType.Graph);

        Assert.Single(definitions);
        Assert.Single(graphs);
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        _registry.RegisterWorkflowDefinition(CreateDefinition("wf-1", "Test", "1.0.0"), "user");

        _registry.UpdateStatus("wf-1", WorkflowRegistryRecordStatus.Deprecated);

        var record = _registry.GetWorkflow("wf-1");
        Assert.NotNull(record);
        Assert.Equal(WorkflowRegistryRecordStatus.Deprecated, record.Status);
    }

    [Fact]
    public void UpdateStatus_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _registry.UpdateStatus("nonexistent", WorkflowRegistryRecordStatus.Archived));
    }

    [Fact]
    public void RegisterDuplicate_ThrowsInvalidOperationException()
    {
        var definition = CreateDefinition("wf-1", "Test", "1.0.0");
        _registry.RegisterWorkflowDefinition(definition, "user");

        Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterWorkflowDefinition(definition, "user"));
    }

    [Fact]
    public void RegisterDuplicateVersion_ThrowsInvalidOperationException()
    {
        _registry.RegisterWorkflowDefinition(CreateDefinition("wf-1", "Test", "1.0.0"), "user");

        Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterWorkflowDefinition(CreateDefinition("wf-2", "Test", "1.0.0"), "user"));
    }

    [Fact]
    public void ConcurrentRegistration_IsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() =>
                _registry.RegisterWorkflowDefinition(
                    CreateDefinition($"wf-{i}", $"Workflow{i}", "1.0.0"), "user")));

        Task.WaitAll(tasks.ToArray());

        Assert.Equal(50, _registry.ListWorkflows().Count);
    }

    [Fact]
    public void WorkflowMetadata_IsPreserved()
    {
        var definition = CreateDefinition("wf-meta", "MetaWorkflow", "3.2.1");
        var record = _registry.RegisterWorkflowDefinition(definition, "admin-user");

        Assert.Equal("wf-meta", record.WorkflowId);
        Assert.Equal("MetaWorkflow", record.WorkflowName);
        Assert.Equal("3.2.1", record.WorkflowVersion);
        Assert.Equal("admin-user", record.CreatedBy);
        Assert.True(record.CreatedAt <= DateTimeOffset.UtcNow);
    }

    private static WorkflowDefinition CreateDefinition(string id, string name, string version)
    {
        return new WorkflowDefinition(
            WorkflowId: id,
            Name: name,
            Description: $"Test workflow {name}",
            Version: version,
            Steps: Array.Empty<Whycespace.Contracts.Workflows.WorkflowStep>(),
            CreatedAt: DateTimeOffset.UtcNow
        );
    }
}
