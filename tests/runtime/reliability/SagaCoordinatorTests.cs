using Whycespace.Reliability.Saga;

namespace Whycespace.Reliability.Tests;

public class SagaCoordinatorTests
{
    [Fact]
    public async Task ExecuteSaga_All_Steps_Succeed()
    {
        var coordinator = new SagaCoordinator();

        var steps = new List<SagaStep>
        {
            new("Step1", () => Task.FromResult(true), () => Task.CompletedTask),
            new("Step2", () => Task.FromResult(true), () => Task.CompletedTask)
        };

        var sagaId = coordinator.StartSaga(steps);
        var result = await coordinator.ExecuteSagaAsync(sagaId);

        Assert.True(result);
        Assert.Equal(SagaStatus.Completed, coordinator.GetStatus(sagaId));
    }

    [Fact]
    public async Task ExecuteSaga_Step_Fails_Triggers_Compensation()
    {
        var compensated = new List<string>();
        var coordinator = new SagaCoordinator();

        var steps = new List<SagaStep>
        {
            new("Step1", () => Task.FromResult(true), () => { compensated.Add("Step1"); return Task.CompletedTask; }),
            new("Step2", () => Task.FromResult(false), () => { compensated.Add("Step2"); return Task.CompletedTask; })
        };

        var sagaId = coordinator.StartSaga(steps);
        var result = await coordinator.ExecuteSagaAsync(sagaId);

        Assert.False(result);
        Assert.Equal(SagaStatus.Compensated, coordinator.GetStatus(sagaId));
        Assert.Single(compensated);
        Assert.Equal("Step1", compensated[0]);
    }

    [Fact]
    public void StartSaga_Creates_Pending_Status()
    {
        var coordinator = new SagaCoordinator();
        var sagaId = coordinator.StartSaga(new List<SagaStep>());

        Assert.Equal(SagaStatus.Pending, coordinator.GetStatus(sagaId));
    }
}
