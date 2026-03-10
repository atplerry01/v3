using Whycespace.Observability.Diagnostics;

namespace Whycespace.Observability.Tests;

public class WorkflowDiagnosticsTests
{
    [Fact]
    public void RecordWorkflowStart_CapturesEntry()
    {
        var service = new WorkflowDiagnosticsService();
        var id = Guid.NewGuid();

        service.RecordWorkflowStart(id, "RideRequestWorkflow", "driver-123");

        var entries = service.GetEntriesForWorkflow(id);
        Assert.Single(entries);
        Assert.Equal("Started", entries[0].Status);
        Assert.Equal("RideRequestWorkflow", entries[0].WorkflowName);
    }

    [Fact]
    public void RecordWorkflowStep_CapturesStepExecution()
    {
        var service = new WorkflowDiagnosticsService();
        var id = Guid.NewGuid();

        service.RecordWorkflowStart(id, "RideRequestWorkflow", "driver-123");
        service.RecordWorkflowStep(id, "RideRequestWorkflow", "DriverMatchingEngine", "driver-123");

        var entries = service.GetEntriesForWorkflow(id);
        Assert.Equal(2, entries.Count);
        Assert.Equal("Executing", entries[1].Status);
        Assert.Equal("DriverMatchingEngine", entries[1].StepName);
    }

    [Fact]
    public void RecordWorkflowFailure_CapturesFailure()
    {
        var service = new WorkflowDiagnosticsService();
        var id = Guid.NewGuid();

        service.RecordWorkflowStart(id, "RideRequestWorkflow", "driver-123");
        service.RecordWorkflowFailure(id, "RideRequestWorkflow", "DriverMatchingEngine", "driver-123");

        var entries = service.GetEntriesForWorkflow(id);
        Assert.Equal(2, entries.Count);
        Assert.Equal("Failed", entries[1].Status);
    }

    [Fact]
    public void RecordWorkflowCompletion_CapturesCompletion()
    {
        var service = new WorkflowDiagnosticsService();
        var id = Guid.NewGuid();

        service.RecordWorkflowStart(id, "RideRequestWorkflow", "driver-123");
        service.RecordWorkflowCompletion(id, "RideRequestWorkflow", "driver-123");

        var entries = service.GetEntriesForWorkflow(id);
        Assert.Equal(2, entries.Count);
        Assert.Equal("Completed", entries[1].Status);
    }
}
