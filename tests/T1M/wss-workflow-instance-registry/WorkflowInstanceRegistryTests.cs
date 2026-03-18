using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;
using WorkflowInstanceStatus = Whycespace.Systems.Midstream.WSS.Execution.WorkflowInstanceStatus;

namespace Whycespace.WSS.WorkflowInstanceRegistry.Tests;

public class WorkflowInstanceRegistryTests
{
    private readonly InstanceRegistryStoreAdapter _store;
    private readonly Whycespace.Engines.T1M.WSS.Registry.WorkflowInstanceRegistry _registry;

    public WorkflowInstanceRegistryTests()
    {
        _store = new InstanceRegistryStoreAdapter();
        _registry = new Whycespace.Engines.T1M.WSS.Registry.WorkflowInstanceRegistry(_store);
    }

    // 1. Create workflow instance
    [Fact]
    public void CreateInstance_ShouldReturnInstanceWithCreatedStatus()
    {
        var context = new Dictionary<string, object> { ["region"] = "us-east" };

        var instance = _registry.CreateInstance("taxi-request", "1.0.0", context);

        Assert.StartsWith("wf-taxi-request-", instance.InstanceId);
        Assert.Equal("taxi-request", instance.WorkflowId);
        Assert.Equal("1.0.0", instance.WorkflowVersion);
        Assert.Equal(string.Empty, instance.CurrentStep);
        Assert.Equal(WorkflowInstanceStatus.Created, instance.Status);
        Assert.Null(instance.CompletedAt);
        Assert.Equal("us-east", instance.Context["region"]);
    }

    // 2. Retrieve instance
    [Fact]
    public void GetInstance_ExistingInstance_ShouldReturnInstance()
    {
        var instance = _registry.CreateInstance("taxi-request", "1.0.0", null);

        var retrieved = _registry.GetInstance(instance.InstanceId);

        Assert.Equal(instance.InstanceId, retrieved.InstanceId);
        Assert.Equal("taxi-request", retrieved.WorkflowId);
    }

    // 3. List instances
    [Fact]
    public void ListInstances_ShouldReturnAllInstances()
    {
        _registry.CreateInstance("wf-a", "1.0.0", null);
        _registry.CreateInstance("wf-b", "2.0.0", null);
        _registry.CreateInstance("wf-c", "1.0.0", null);

        var instances = _registry.ListInstances();

        Assert.Equal(3, instances.Count);
    }

    // 4. Update instance state
    [Fact]
    public void UpdateInstanceState_ShouldUpdateStepAndStatus()
    {
        var instance = _registry.CreateInstance("taxi-request", "1.0.0", null);

        var updated = _registry.UpdateInstanceState(instance.InstanceId, "find-driver", WorkflowInstanceStatus.Running);

        Assert.Equal("find-driver", updated.CurrentStep);
        Assert.Equal(WorkflowInstanceStatus.Running, updated.Status);
        Assert.Null(updated.CompletedAt);
    }

    [Fact]
    public void UpdateInstanceState_TerminalStatus_ShouldSetCompletedAt()
    {
        var instance = _registry.CreateInstance("taxi-request", "1.0.0", null);

        var completed = _registry.UpdateInstanceState(instance.InstanceId, "end", WorkflowInstanceStatus.Completed);

        Assert.Equal(WorkflowInstanceStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    // 5. Remove instance
    [Fact]
    public void RemoveInstance_ShouldRemoveFromRegistry()
    {
        var instance = _registry.CreateInstance("taxi-request", "1.0.0", null);

        _registry.RemoveInstance(instance.InstanceId);

        Assert.Throws<KeyNotFoundException>(() => _registry.GetInstance(instance.InstanceId));
    }

    // 6. Retrieve missing instance
    [Fact]
    public void GetInstance_MissingInstance_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _registry.GetInstance("wf-nonexistent-20260312-abc"));
    }

    // 7. Concurrent instance creation
    [Fact]
    public async Task CreateInstance_ConcurrentCreation_ShouldCreateAllInstances()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => _registry.CreateInstance($"wf-{i}", "1.0.0", null))).ToArray();

        await Task.WhenAll(tasks);

        var instances = _registry.ListInstances();
        Assert.Equal(50, instances.Count);
    }
}
