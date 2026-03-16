namespace Whycespace.Systems.Midstream.WSS.Registry;


public interface IWorkflowRegistryStore
{
    void Save(WorkflowRegistryRecord record);

    void Update(WorkflowRegistryRecord record);

    WorkflowRegistryRecord? GetById(string workflowId);

    WorkflowRegistryRecord? GetByName(string workflowName);

    WorkflowRegistryRecord? GetByNameAndVersion(string workflowName, string version);

    IReadOnlyList<WorkflowRegistryRecord> GetAll();

    IReadOnlyList<WorkflowRegistryRecord> GetByType(WorkflowType type);

    bool ExistsById(string workflowId);

    bool ExistsByNameAndVersion(string workflowName, string version);
}
