namespace Whycespace.Runtime.Persistence.Abstractions;

using Whycespace.Contracts.Workflows;

public interface IWorkflowStateRepository
{
    Task InitializeAsync();
    Task SaveAsync(WorkflowState state);
    Task<WorkflowState?> GetAsync(string workflowId);
    Task<IReadOnlyList<WorkflowState>> GetByStatusAsync(WorkflowStatus status);
}
