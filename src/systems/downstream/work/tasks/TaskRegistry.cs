namespace Whycespace.Systems.Downstream.Work.Tasks;

public sealed class TaskRegistry
{
    private readonly Dictionary<Guid, TaskDefinition> _tasks = new();
    private readonly Dictionary<string, List<Guid>> _clusterIndex = new();

    public void Register(TaskDefinition task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (_tasks.ContainsKey(task.TaskId))
            throw new InvalidOperationException($"Task '{task.TaskId}' is already registered.");

        _tasks[task.TaskId] = task;

        if (!_clusterIndex.TryGetValue(task.ClusterId, out var list))
        {
            list = new List<Guid>();
            _clusterIndex[task.ClusterId] = list;
        }
        list.Add(task.TaskId);
    }

    public TaskDefinition? Get(Guid taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return task;
    }

    public IReadOnlyList<TaskDefinition> GetTasksByCluster(string clusterId)
    {
        if (!_clusterIndex.TryGetValue(clusterId, out var ids))
            return [];

        return ids.Select(id => _tasks[id]).ToList();
    }

    public IReadOnlyList<TaskDefinition> ListTasks() => _tasks.Values.ToList();
}
