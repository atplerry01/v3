namespace Whycespace.WorkflowRuntime.Tests;

using Whycespace.Contracts.Workflows;
using Whycespace.WorkflowRuntime.Registry;

public class WorkflowRegistryTests
{
    private readonly WorkflowRegistry _registry = new();

    private static WorkflowGraph CreateGraph(string name) => new(
        WorkflowId: Guid.NewGuid().ToString(),
        Name: name,
        Steps: new[]
        {
            new WorkflowStep("step-1", "Step 1", "TestEngine", Array.Empty<string>())
        });

    [Fact]
    public void Register_ThenResolve_ReturnsGraph()
    {
        var graph = CreateGraph("TestWorkflow");
        _registry.Register(graph);

        var resolved = _registry.Resolve("TestWorkflow");
        Assert.NotNull(resolved);
        Assert.Equal("TestWorkflow", resolved.Name);
    }

    [Fact]
    public void Resolve_UnknownWorkflow_ReturnsNull()
    {
        Assert.Null(_registry.Resolve("NonExistent"));
    }

    [Fact]
    public void Register_OverwritesExisting()
    {
        var graph1 = CreateGraph("Workflow");
        var graph2 = CreateGraph("Workflow");
        _registry.Register(graph1);
        _registry.Register(graph2);

        var resolved = _registry.Resolve("Workflow");
        Assert.Equal(graph2.WorkflowId, resolved!.WorkflowId);
    }

    [Fact]
    public void GetRegisteredWorkflows_ReturnsAllNames()
    {
        _registry.Register(CreateGraph("A"));
        _registry.Register(CreateGraph("B"));
        _registry.Register(CreateGraph("C"));

        var names = _registry.GetRegisteredWorkflows();
        Assert.Equal(3, names.Count);
        Assert.Contains("A", names);
        Assert.Contains("B", names);
        Assert.Contains("C", names);
    }
}
