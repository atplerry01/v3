using Whycespace.Contracts.Primitives;
using Whycespace.Reliability.Models;
using Whycespace.Reliability.State;

namespace Whycespace.Reliability.Tests;

public class WorkflowStateStoreTests
{
    [Fact]
    public async Task SaveAsync_And_LoadAsync_Roundtrips()
    {
        var store = new InMemoryWorkflowStateStore();
        var id = Guid.NewGuid();

        var entry = new WorkflowStateEntry(
            id, "TestWorkflow", 2,
            new PartitionKey("pk-1"),
            new Dictionary<string, object> { ["key"] = "value" }
        );

        await store.SaveAsync(entry);
        var loaded = await store.LoadAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("TestWorkflow", loaded.WorkflowName);
        Assert.Equal(2, loaded.CurrentStepIndex);
    }

    [Fact]
    public async Task LoadAsync_Returns_Null_For_Unknown_Id()
    {
        var store = new InMemoryWorkflowStateStore();
        var result = await store.LoadAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveWorkflowsAsync_Returns_All_Entries()
    {
        var store = new InMemoryWorkflowStateStore();

        await store.SaveAsync(new WorkflowStateEntry(
            Guid.NewGuid(), "WF1", 0, new PartitionKey("pk"), new Dictionary<string, object>()));
        await store.SaveAsync(new WorkflowStateEntry(
            Guid.NewGuid(), "WF2", 1, new PartitionKey("pk"), new Dictionary<string, object>()));

        var active = await store.GetActiveWorkflowsAsync();
        Assert.Equal(2, active.Count);
    }
}
