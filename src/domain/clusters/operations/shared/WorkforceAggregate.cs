namespace Whycespace.Domain.Clusters.Operations.Shared;

public sealed class WorkforceAggregate
{
    private readonly List<string> _capabilities = new();
    private readonly List<ScheduleRecord> _schedules = new();
    private readonly List<object> _domainEvents = new();

    public WorkerId WorkerId { get; }
    public string Name { get; }
    public WorkerStatus Status { get; private set; }
    public WorkerAvailability Availability { get; private set; }
    public IReadOnlyList<string> Capabilities => _capabilities.AsReadOnly();
    public IReadOnlyList<ScheduleRecord> Schedules => _schedules.AsReadOnly();
    public Guid? CurrentTaskId { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private WorkforceAggregate(
        WorkerId workerId,
        string name,
        IEnumerable<string> capabilities)
    {
        WorkerId = workerId;
        Name = name;
        Status = WorkerStatus.Active;
        Availability = WorkerAvailability.Available;
        _capabilities.AddRange(capabilities);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static WorkforceAggregate Register(
        WorkerId workerId,
        string name,
        IEnumerable<string> capabilities)
    {
        if (workerId == WorkerId.Empty)
            throw new InvalidOperationException("WorkerId must exist.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Worker name is required.");

        return new WorkforceAggregate(workerId, name, capabilities);
    }

    public bool HasCapability(string capability)
        => _capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

    public bool IsEligible() => Status == WorkerStatus.Active;

    public bool IsAvailable() => Availability == WorkerAvailability.Available;

    public bool IsAssignedTo(Guid taskId) => CurrentTaskId == taskId;

    public void AssignToTask(Guid taskId)
    {
        if (Status != WorkerStatus.Active)
            throw new InvalidOperationException("Only active workers can be assigned.");

        if (Availability != WorkerAvailability.Available)
            throw new InvalidOperationException("Worker is not available for assignment.");

        CurrentTaskId = taskId;
        Availability = WorkerAvailability.Busy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseFromTask()
    {
        CurrentTaskId = null;
        Availability = WorkerAvailability.Available;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        if (Status != WorkerStatus.Active)
            throw new InvalidOperationException("Only active workers can be suspended.");

        Status = WorkerStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == WorkerStatus.Active)
            throw new InvalidOperationException("Worker is already active.");

        if (Status == WorkerStatus.Terminated)
            throw new InvalidOperationException("Terminated workers cannot be activated.");

        Status = WorkerStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetUnavailable()
    {
        if (Status != WorkerStatus.Active)
            throw new InvalidOperationException("Only active workers can be set to unavailable.");

        Status = WorkerStatus.Unavailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Terminate()
    {
        if (Status != WorkerStatus.Active)
            throw new InvalidOperationException("Only active workers can be terminated.");

        Status = WorkerStatus.Terminated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(WorkerStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasScheduleOverlap(DateTimeOffset start, DateTimeOffset end)
        => _schedules.Any(s => s.Start < end && s.End > start);

    public void AddSchedule(ScheduleRecord schedule)
    {
        if (HasScheduleOverlap(schedule.Start, schedule.End))
            throw new InvalidOperationException("Schedule overlaps with an existing schedule.");

        _schedules.Add(schedule);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
