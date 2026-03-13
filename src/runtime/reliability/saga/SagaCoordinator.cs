namespace Whycespace.Reliability.Saga;

public sealed class SagaCoordinator
{
    private readonly Dictionary<Guid, SagaState> _sagas = new();

    public Guid StartSaga(IReadOnlyList<SagaStep> steps)
    {
        var sagaId = Guid.NewGuid();
        _sagas[sagaId] = new SagaState(sagaId, steps);
        return sagaId;
    }

    public async Task<bool> ExecuteSagaAsync(Guid sagaId)
    {
        if (!_sagas.TryGetValue(sagaId, out var state))
            throw new InvalidOperationException($"Saga {sagaId} not found.");

        var completed = new List<SagaStep>();

        foreach (var step in state.Steps)
        {
            var success = await step.Execute();
            if (!success)
            {
                for (var i = completed.Count - 1; i >= 0; i--)
                    await completed[i].Compensate();

                state.Status = SagaStatus.Compensated;
                return false;
            }

            completed.Add(step);
        }

        state.Status = SagaStatus.Completed;
        return true;
    }

    public SagaStatus GetStatus(Guid sagaId)
    {
        return _sagas.TryGetValue(sagaId, out var state)
            ? state.Status
            : throw new InvalidOperationException($"Saga {sagaId} not found.");
    }

    private sealed class SagaState
    {
        public Guid SagaId { get; }
        public IReadOnlyList<SagaStep> Steps { get; }
        public SagaStatus Status { get; set; }

        public SagaState(Guid sagaId, IReadOnlyList<SagaStep> steps)
        {
            SagaId = sagaId;
            Steps = steps;
            Status = SagaStatus.Pending;
        }
    }
}

public enum SagaStatus
{
    Pending,
    Completed,
    Compensated
}
