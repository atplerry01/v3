namespace Whycespace.Runtime.Reliability;

public sealed record SagaStep(string Name, Func<Task<bool>> Execute, Func<Task> Compensate);

public sealed class SagaCoordinator
{
    public async Task<bool> ExecuteSagaAsync(IReadOnlyList<SagaStep> steps)
    {
        var completed = new List<SagaStep>();

        foreach (var step in steps)
        {
            var success = await step.Execute();
            if (!success)
            {
                for (var i = completed.Count - 1; i >= 0; i--)
                    await completed[i].Compensate();
                return false;
            }
            completed.Add(step);
        }
        return true;
    }
}
