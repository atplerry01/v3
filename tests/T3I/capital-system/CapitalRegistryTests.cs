namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Domain.Core.Economic;

public sealed class CapitalRegistryTests
{
    private readonly CapitalRegistry _registry = new();

    private static CapitalRecord CreateRecord(
        Guid? capitalId = null,
        CapitalType capitalType = CapitalType.Contribution,
        Guid? poolId = null,
        Guid? ownerId = null,
        Guid? clusterId = null,
        Guid? subClusterId = null,
        Guid? spvId = null,
        decimal amount = 1000m,
        string currency = "GBP",
        CapitalStatus status = CapitalStatus.Registered)
    {
        return new CapitalRecord(
            CapitalId: capitalId ?? Guid.NewGuid(),
            CapitalType: capitalType,
            PoolId: poolId ?? Guid.NewGuid(),
            OwnerIdentityId: ownerId ?? Guid.NewGuid(),
            ClusterId: clusterId ?? Guid.NewGuid(),
            SubClusterId: subClusterId ?? Guid.NewGuid(),
            SPVId: spvId ?? Guid.NewGuid(),
            Amount: amount,
            Currency: currency,
            Status: status,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterCapital_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterCapital(record);

        var result = _registry.GetCapital(record.CapitalId);
        Assert.NotNull(result);
        Assert.Equal(record.CapitalId, result.CapitalId);
        Assert.Equal(record.Amount, result.Amount);
    }

    [Fact]
    public void RegisterCapital_DuplicateId_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterCapital(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterCapital(record));
    }

    [Fact]
    public void RegisterCapital_EmptyOwner_Throws()
    {
        var record = CreateRecord(ownerId: Guid.Empty);

        Assert.Throws<ArgumentException>(() => _registry.RegisterCapital(record));
    }

    [Fact]
    public void GetCapital_ReturnsCorrectRecord()
    {
        var record = CreateRecord();
        _registry.RegisterCapital(record);

        var result = _registry.GetCapital(record.CapitalId);

        Assert.NotNull(result);
        Assert.Equal(record.OwnerIdentityId, result.OwnerIdentityId);
        Assert.Equal(record.CapitalType, result.CapitalType);
        Assert.Equal(record.Currency, result.Currency);
    }

    [Fact]
    public void GetCapital_NonExistent_ReturnsNull()
    {
        var result = _registry.GetCapital(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void ListCapitalByOwner_ReturnsMatchingRecords()
    {
        var ownerId = Guid.NewGuid();
        var record1 = CreateRecord(ownerId: ownerId);
        var record2 = CreateRecord(ownerId: ownerId);
        var record3 = CreateRecord(); // different owner

        _registry.RegisterCapital(record1);
        _registry.RegisterCapital(record2);
        _registry.RegisterCapital(record3);

        var results = _registry.ListCapitalByOwner(ownerId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(ownerId, r.OwnerIdentityId));
    }

    [Fact]
    public void ListCapitalByOwner_NoMatch_ReturnsEmpty()
    {
        var results = _registry.ListCapitalByOwner(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void ListCapitalByPool_ReturnsMatchingRecords()
    {
        var poolId = Guid.NewGuid();
        var record1 = CreateRecord(poolId: poolId);
        var record2 = CreateRecord(poolId: poolId);
        var record3 = CreateRecord(); // different pool

        _registry.RegisterCapital(record1);
        _registry.RegisterCapital(record2);
        _registry.RegisterCapital(record3);

        var results = _registry.ListCapitalByPool(poolId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(poolId, r.PoolId));
    }

    [Fact]
    public void ListCapitalByPool_NoMatch_ReturnsEmpty()
    {
        var results = _registry.ListCapitalByPool(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void ListCapitalBySPV_ReturnsMatchingRecords()
    {
        var spvId = Guid.NewGuid();
        var record1 = CreateRecord(spvId: spvId);
        var record2 = CreateRecord(spvId: spvId);
        var record3 = CreateRecord(); // different SPV

        _registry.RegisterCapital(record1);
        _registry.RegisterCapital(record2);
        _registry.RegisterCapital(record3);

        var results = _registry.ListCapitalBySPV(spvId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(spvId, r.SPVId));
    }

    [Fact]
    public void ListCapitalBySPV_NoMatch_ReturnsEmpty()
    {
        var results = _registry.ListCapitalBySPV(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateCapitalStatus_UpdatesSuccessfully()
    {
        var record = CreateRecord(status: CapitalStatus.Registered);
        _registry.RegisterCapital(record);

        _registry.UpdateCapitalStatus(record.CapitalId, CapitalStatus.Allocated);

        var result = _registry.GetCapital(record.CapitalId);
        Assert.NotNull(result);
        Assert.Equal(CapitalStatus.Allocated, result.Status);
    }

    [Fact]
    public void UpdateCapitalStatus_NonExistent_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => _registry.UpdateCapitalStatus(Guid.NewGuid(), CapitalStatus.Closed));
    }

    [Fact]
    public void UpdateCapitalStatus_UpdatesTimestamp()
    {
        var record = CreateRecord();
        _registry.RegisterCapital(record);
        var originalUpdatedAt = record.UpdatedAt;

        _registry.UpdateCapitalStatus(record.CapitalId, CapitalStatus.Utilized);

        var result = _registry.GetCapital(record.CapitalId);
        Assert.NotNull(result);
        Assert.True(result.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async Task ConcurrentRegistration_IsThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _registry.RegisterCapital(CreateRecord());
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
}
