using Whycespace.Reliability.Models;

namespace Whycespace.Reliability.State;

public interface IWorkflowStateStore
{
    Task SaveAsync(WorkflowStateEntry entry);

    Task<WorkflowStateEntry?> LoadAsync(Guid workflowInstanceId);

    Task<IReadOnlyCollection<WorkflowStateEntry>> GetActiveWorkflowsAsync();
}
