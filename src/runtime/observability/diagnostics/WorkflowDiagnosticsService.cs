using System.Collections.Concurrent;
using Whycespace.Observability.Models;

namespace Whycespace.Observability.Diagnostics;

public sealed class WorkflowDiagnosticsService
{
    private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();

    public void RecordWorkflowStart(Guid workflowId, string workflowName, string partitionKey)
    {
        _entries.Enqueue(new DiagnosticEntry(
            workflowId, workflowName, "Start", partitionKey, "Started", DateTime.UtcNow));
    }

    public void RecordWorkflowStep(Guid workflowId, string workflowName, string stepName, string partitionKey)
    {
        _entries.Enqueue(new DiagnosticEntry(
            workflowId, workflowName, stepName, partitionKey, "Executing", DateTime.UtcNow));
    }

    public void RecordWorkflowCompletion(Guid workflowId, string workflowName, string partitionKey)
    {
        _entries.Enqueue(new DiagnosticEntry(
            workflowId, workflowName, "Complete", partitionKey, "Completed", DateTime.UtcNow));
    }

    public void RecordWorkflowFailure(Guid workflowId, string workflowName, string stepName, string partitionKey)
    {
        _entries.Enqueue(new DiagnosticEntry(
            workflowId, workflowName, stepName, partitionKey, "Failed", DateTime.UtcNow));
    }

    public IReadOnlyList<DiagnosticEntry> GetEntries()
    {
        return _entries.ToList();
    }

    public IReadOnlyList<DiagnosticEntry> GetEntriesForWorkflow(Guid workflowId)
    {
        return _entries.Where(e => e.WorkflowId == workflowId).ToList();
    }
}
