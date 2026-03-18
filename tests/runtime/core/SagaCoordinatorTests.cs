namespace Whycespace.Tests.Runtime;

using Whycespace.Runtime.Reliability;
using Xunit;

public sealed class SagaCoordinatorTests
{
    [Fact]
    public async Task AllStepsSucceed_ReturnsTrue()
    {
        var coordinator = new SagaCoordinator();
        var executed = new List<string>();

        var steps = new List<SagaStep>
        {
            new("Step1", async () => { executed.Add("E1"); return await Task.FromResult(true); }, () => Task.CompletedTask),
            new("Step2", async () => { executed.Add("E2"); return await Task.FromResult(true); }, () => Task.CompletedTask),
            new("Step3", async () => { executed.Add("E3"); return await Task.FromResult(true); }, () => Task.CompletedTask)
        };

        var result = await coordinator.ExecuteSagaAsync(steps);

        Assert.True(result);
        Assert.Equal(new[] { "E1", "E2", "E3" }, executed);
    }

    [Fact]
    public async Task SecondStepFails_CompensatesFirstStep()
    {
        var coordinator = new SagaCoordinator();
        var compensated = new List<string>();

        var steps = new List<SagaStep>
        {
            new("Step1",
                () => Task.FromResult(true),
                async () => { compensated.Add("C1"); await Task.CompletedTask; }),
            new("Step2",
                () => Task.FromResult(false),
                async () => { compensated.Add("C2"); await Task.CompletedTask; }),
            new("Step3",
                () => Task.FromResult(true),
                async () => { compensated.Add("C3"); await Task.CompletedTask; })
        };

        var result = await coordinator.ExecuteSagaAsync(steps);

        Assert.False(result);
        Assert.Single(compensated);
        Assert.Equal("C1", compensated[0]);
    }

    [Fact]
    public async Task ThirdStepFails_CompensatesInReverseOrder()
    {
        var coordinator = new SagaCoordinator();
        var compensated = new List<string>();

        var steps = new List<SagaStep>
        {
            new("Step1",
                () => Task.FromResult(true),
                async () => { compensated.Add("C1"); await Task.CompletedTask; }),
            new("Step2",
                () => Task.FromResult(true),
                async () => { compensated.Add("C2"); await Task.CompletedTask; }),
            new("Step3",
                () => Task.FromResult(false),
                async () => { compensated.Add("C3"); await Task.CompletedTask; })
        };

        var result = await coordinator.ExecuteSagaAsync(steps);

        Assert.False(result);
        Assert.Equal(2, compensated.Count);
        Assert.Equal("C2", compensated[0]);
        Assert.Equal("C1", compensated[1]);
    }

    [Fact]
    public async Task EmptySaga_ReturnsTrue()
    {
        var coordinator = new SagaCoordinator();
        var result = await coordinator.ExecuteSagaAsync(Array.Empty<SagaStep>());
        Assert.True(result);
    }
}
