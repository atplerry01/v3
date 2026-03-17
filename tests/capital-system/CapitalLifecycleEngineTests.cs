namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Engines.T3I.Forecasting.Economic;

public sealed class CapitalLifecycleEngineTests
{
    private readonly CapitalLifecycleEngine _engine = new();

    private static TrackCapitalLifecycleCommand CreateCommand(
        CapitalLifecycleStage previousStage = CapitalLifecycleStage.Commitment,
        CapitalLifecycleStage newStage = CapitalLifecycleStage.Contribution,
        Guid? capitalId = null,
        Guid? referenceId = null,
        Guid? triggeredBy = null)
    {
        return new TrackCapitalLifecycleCommand(
            CapitalId: capitalId ?? Guid.NewGuid(),
            PreviousStage: previousStage,
            NewStage: newStage,
            ReferenceId: referenceId ?? Guid.NewGuid(),
            TriggeredBy: triggeredBy ?? Guid.NewGuid(),
            TriggeredAt: DateTimeOffset.UtcNow);
    }

    // --- Valid Lifecycle Transitions ---

    [Theory]
    [InlineData(CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Contribution)]
    [InlineData(CapitalLifecycleStage.Contribution, CapitalLifecycleStage.Reservation)]
    [InlineData(CapitalLifecycleStage.Reservation, CapitalLifecycleStage.Allocation)]
    [InlineData(CapitalLifecycleStage.Allocation, CapitalLifecycleStage.Utilization)]
    [InlineData(CapitalLifecycleStage.Utilization, CapitalLifecycleStage.Distribution)]
    [InlineData(CapitalLifecycleStage.Distribution, CapitalLifecycleStage.Closed)]
    public void ValidLifecycleTransition_Succeeds(
        CapitalLifecycleStage from, CapitalLifecycleStage to)
    {
        var command = CreateCommand(previousStage: from, newStage: to);

        var result = _engine.TrackLifecycle(command);

        Assert.True(result.Success);
        Assert.NotNull(result.Record);
        Assert.Equal(from, result.Record.PreviousStage);
        Assert.Equal(to, result.Record.CurrentStage);
        Assert.Equal(command.CapitalId, result.Record.CapitalId);
        Assert.Equal(command.ReferenceId, result.Record.ReferenceId);
        Assert.Equal(command.TriggeredAt, result.Record.Timestamp);
    }

    // --- Invalid Lifecycle Transitions ---

    [Theory]
    [InlineData(CapitalLifecycleStage.Contribution, CapitalLifecycleStage.Utilization)]
    [InlineData(CapitalLifecycleStage.Reservation, CapitalLifecycleStage.Distribution)]
    [InlineData(CapitalLifecycleStage.Allocation, CapitalLifecycleStage.Contribution)]
    [InlineData(CapitalLifecycleStage.Closed, CapitalLifecycleStage.Commitment)]
    [InlineData(CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Allocation)]
    [InlineData(CapitalLifecycleStage.Utilization, CapitalLifecycleStage.Reservation)]
    public void InvalidLifecycleTransition_Fails(
        CapitalLifecycleStage from, CapitalLifecycleStage to)
    {
        var command = CreateCommand(previousStage: from, newStage: to);

        var result = _engine.TrackLifecycle(command);

        Assert.False(result.Success);
        Assert.Null(result.Record);
        Assert.Contains("Invalid lifecycle transition", result.Error);
    }

    // --- Lifecycle History Tracking ---

    [Fact]
    public void LifecycleHistoryTracking_FullChain_ProducesCorrectRecords()
    {
        var capitalId = Guid.NewGuid();
        var transitions = new[]
        {
            (CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Contribution),
            (CapitalLifecycleStage.Contribution, CapitalLifecycleStage.Reservation),
            (CapitalLifecycleStage.Reservation, CapitalLifecycleStage.Allocation),
            (CapitalLifecycleStage.Allocation, CapitalLifecycleStage.Utilization),
            (CapitalLifecycleStage.Utilization, CapitalLifecycleStage.Distribution),
            (CapitalLifecycleStage.Distribution, CapitalLifecycleStage.Closed),
        };

        var records = new List<CapitalLifecycleRecord>();

        foreach (var (from, to) in transitions)
        {
            var command = CreateCommand(previousStage: from, newStage: to, capitalId: capitalId);
            var result = _engine.TrackLifecycle(command);

            Assert.True(result.Success);
            Assert.NotNull(result.Record);
            records.Add(result.Record);
        }

        Assert.Equal(6, records.Count);
        Assert.All(records, r => Assert.Equal(capitalId, r.CapitalId));
        Assert.Equal(CapitalLifecycleStage.Commitment, records[0].PreviousStage);
        Assert.Equal(CapitalLifecycleStage.Closed, records[^1].CurrentStage);
    }

    // --- Concurrent Lifecycle Tracking ---

    [Fact]
    public async Task ConcurrentLifecycleTracking_IsThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var command = CreateCommand();
                    var result = _engine.TrackLifecycle(command);
                    Assert.True(result.Success);
                    Assert.NotNull(result.Record);
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
    }

    // --- Validation ---

    [Fact]
    public void EmptyCapitalId_Fails()
    {
        var command = CreateCommand(capitalId: Guid.Empty);

        var result = _engine.TrackLifecycle(command);

        Assert.False(result.Success);
        Assert.Contains("CapitalId", result.Error);
    }

    [Fact]
    public void EmptyReferenceId_Fails()
    {
        var command = CreateCommand(referenceId: Guid.Empty);

        var result = _engine.TrackLifecycle(command);

        Assert.False(result.Success);
        Assert.Contains("ReferenceId", result.Error);
    }

    [Fact]
    public void EmptyTriggeredBy_Fails()
    {
        var command = CreateCommand(triggeredBy: Guid.Empty);

        var result = _engine.TrackLifecycle(command);

        Assert.False(result.Success);
        Assert.Contains("TriggeredBy", result.Error);
    }

    [Fact]
    public void SameStageTransition_Fails()
    {
        var command = CreateCommand(
            previousStage: CapitalLifecycleStage.Allocation,
            newStage: CapitalLifecycleStage.Allocation);

        var result = _engine.TrackLifecycle(command);

        Assert.False(result.Success);
        Assert.Contains("must be different", result.Error);
    }

    // --- Determinism ---

    [Fact]
    public void DeterministicBehavior_SameInput_SameOutput()
    {
        var capitalId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var triggeredBy = Guid.NewGuid();
        var triggeredAt = DateTimeOffset.UtcNow;

        var command = new TrackCapitalLifecycleCommand(
            capitalId, CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Contribution,
            referenceId, triggeredBy, triggeredAt);

        var result1 = _engine.TrackLifecycle(command);
        var result2 = _engine.TrackLifecycle(command);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Record, result2.Record);
    }

    // --- IsValidTransition static method ---

    [Fact]
    public void IsValidTransition_ValidPair_ReturnsTrue()
    {
        Assert.True(CapitalLifecycleEngine.IsValidTransition(
            CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Contribution));
    }

    [Fact]
    public void IsValidTransition_InvalidPair_ReturnsFalse()
    {
        Assert.False(CapitalLifecycleEngine.IsValidTransition(
            CapitalLifecycleStage.Commitment, CapitalLifecycleStage.Closed));
    }
}
