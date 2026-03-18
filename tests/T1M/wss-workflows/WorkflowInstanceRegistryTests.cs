namespace Whycespace.WSS.Workflows.Tests;

using Whycespace.Systems.Midstream.WSS.Instances;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Stores;
using Xunit;

public sealed class WorkflowInstanceRegistryTests
{
    private readonly WorkflowInstanceRegistry _registry;
    private readonly WorkflowInstanceStore _store;

    public WorkflowInstanceRegistryTests()
    {
        _store = new WorkflowInstanceStore();
        _registry = new WorkflowInstanceRegistry(_store);
    }

    [Fact]
    public void CreateWorkflowInstance_Succeeds()
    {
        var record = _registry.CreateWorkflowInstance(
            "OrderProcessing", "1.0.0", "corr-001", "admin-user");

        Assert.NotNull(record);
        Assert.NotEmpty(record.InstanceId);
        Assert.Equal("OrderProcessing", record.WorkflowName);
        Assert.Equal("1.0.0", record.WorkflowVersion);
        Assert.Equal("OrderProcessing:1.0.0", record.WorkflowId);
        Assert.Equal(WorkflowInstanceStatus.Created, record.Status);
        Assert.Equal("corr-001", record.CorrelationId);
        Assert.Equal("admin-user", record.InitiatedBy);
        Assert.Null(record.CompletedAt);
        Assert.True(record.StartedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void UpdateWorkflowInstanceStatus_ToRunning_Succeeds()
    {
        var record = _registry.CreateWorkflowInstance(
            "TestWorkflow", "1.0.0", "corr-002", "user");

        _registry.UpdateWorkflowInstanceStatus(record.InstanceId, WorkflowInstanceStatus.Running);

        var resolved = _registry.ResolveWorkflowInstance(record.InstanceId);
        Assert.NotNull(resolved);
        Assert.Equal(WorkflowInstanceStatus.Running, resolved.Status);
        Assert.Null(resolved.CompletedAt);
    }

    [Fact]
    public void UpdateWorkflowInstanceStatus_ToCompleted_SetsCompletedAt()
    {
        var record = _registry.CreateWorkflowInstance(
            "TestWorkflow", "1.0.0", "corr-003", "user");

        _registry.UpdateWorkflowInstanceStatus(record.InstanceId, WorkflowInstanceStatus.Completed);

        var resolved = _registry.ResolveWorkflowInstance(record.InstanceId);
        Assert.NotNull(resolved);
        Assert.Equal(WorkflowInstanceStatus.Completed, resolved.Status);
        Assert.NotNull(resolved.CompletedAt);
    }

    [Fact]
    public void UpdateWorkflowInstanceStatus_ToFailed_SetsCompletedAt()
    {
        var record = _registry.CreateWorkflowInstance(
            "TestWorkflow", "1.0.0", "corr-004", "user");

        _registry.UpdateWorkflowInstanceStatus(record.InstanceId, WorkflowInstanceStatus.Failed);

        var resolved = _registry.ResolveWorkflowInstance(record.InstanceId);
        Assert.NotNull(resolved);
        Assert.Equal(WorkflowInstanceStatus.Failed, resolved.Status);
        Assert.NotNull(resolved.CompletedAt);
    }

    [Fact]
    public void UpdateWorkflowInstanceStatus_ToTerminated_SetsCompletedAt()
    {
        var record = _registry.CreateWorkflowInstance(
            "TestWorkflow", "1.0.0", "corr-005", "user");

        _registry.UpdateWorkflowInstanceStatus(record.InstanceId, WorkflowInstanceStatus.Terminated);

        var resolved = _registry.ResolveWorkflowInstance(record.InstanceId);
        Assert.NotNull(resolved);
        Assert.Equal(WorkflowInstanceStatus.Terminated, resolved.Status);
        Assert.NotNull(resolved.CompletedAt);
    }

    [Fact]
    public void UpdateWorkflowInstanceStatus_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _registry.UpdateWorkflowInstanceStatus("nonexistent", WorkflowInstanceStatus.Running));
    }

    [Fact]
    public void ResolveWorkflowInstance_ById_ReturnsRecord()
    {
        var record = _registry.CreateWorkflowInstance(
            "LookupWorkflow", "2.0.0", "corr-006", "user");

        var resolved = _registry.ResolveWorkflowInstance(record.InstanceId);

        Assert.NotNull(resolved);
        Assert.Equal(record.InstanceId, resolved.InstanceId);
        Assert.Equal("LookupWorkflow", resolved.WorkflowName);
    }

    [Fact]
    public void ResolveWorkflowInstance_NotFound_ReturnsNull()
    {
        var result = _registry.ResolveWorkflowInstance("nonexistent-id");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveByCorrelationId_ReturnsCorrectInstance()
    {
        _registry.CreateWorkflowInstance("Wf1", "1.0.0", "corr-aaa", "user");
        var target = _registry.CreateWorkflowInstance("Wf2", "1.0.0", "corr-bbb", "user");
        _registry.CreateWorkflowInstance("Wf3", "1.0.0", "corr-ccc", "user");

        var resolved = _registry.ResolveByCorrelationId("corr-bbb");

        Assert.NotNull(resolved);
        Assert.Equal(target.InstanceId, resolved.InstanceId);
        Assert.Equal("Wf2", resolved.WorkflowName);
    }

    [Fact]
    public void ResolveByCorrelationId_NotFound_ReturnsNull()
    {
        var result = _registry.ResolveByCorrelationId("nonexistent-corr");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveByWorkflowName_ReturnsAllMatching()
    {
        _registry.CreateWorkflowInstance("OrderFlow", "1.0.0", "corr-010", "user");
        _registry.CreateWorkflowInstance("OrderFlow", "1.0.0", "corr-011", "user");
        _registry.CreateWorkflowInstance("PaymentFlow", "1.0.0", "corr-012", "user");

        var results = _registry.ResolveByWorkflowName("OrderFlow");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("OrderFlow", r.WorkflowName));
    }

    [Fact]
    public void ListActiveWorkflowInstances_ReturnsOnlyActive()
    {
        var active1 = _registry.CreateWorkflowInstance("Wf1", "1.0.0", "corr-020", "user");
        var active2 = _registry.CreateWorkflowInstance("Wf2", "1.0.0", "corr-021", "user");
        var completed = _registry.CreateWorkflowInstance("Wf3", "1.0.0", "corr-022", "user");

        _registry.UpdateWorkflowInstanceStatus(active1.InstanceId, WorkflowInstanceStatus.Running);
        _registry.UpdateWorkflowInstanceStatus(active2.InstanceId, WorkflowInstanceStatus.Waiting);
        _registry.UpdateWorkflowInstanceStatus(completed.InstanceId, WorkflowInstanceStatus.Completed);

        var actives = _registry.ListActiveWorkflowInstances();

        Assert.Equal(2, actives.Count);
        Assert.DoesNotContain(actives, r => r.Status == WorkflowInstanceStatus.Completed);
    }

    [Fact]
    public void ConcurrentInstanceCreation_IsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() =>
                _registry.CreateWorkflowInstance(
                    "ConcurrentWorkflow", "1.0.0", $"corr-{i:D4}", "user")));

        Task.WaitAll(tasks.ToArray());

        var all = _registry.ResolveByWorkflowName("ConcurrentWorkflow");
        Assert.Equal(50, all.Count);
    }

    [Fact]
    public void CreateWorkflowInstance_NullWorkflowName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.CreateWorkflowInstance(null!, "1.0.0", "corr", "user"));
    }

    [Fact]
    public void CreateWorkflowInstance_EmptyCorrelationId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.CreateWorkflowInstance("Wf", "1.0.0", "", "user"));
    }

    [Fact]
    public void CreateWorkflowInstance_EmptyInitiatedBy_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.CreateWorkflowInstance("Wf", "1.0.0", "corr", ""));
    }
}
