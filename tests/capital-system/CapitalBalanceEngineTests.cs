namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Engines.T3I.Economic.Capital;
using Whycespace.Systems.Midstream.Capital.Registry;

public sealed class CapitalBalanceEngineTests
{
    private readonly CapitalBalanceEngine _engine = new();

    private static ComputeCapitalBalanceCommand CreateCommand(
        Guid? poolId = null,
        Guid? investorIdentityId = null,
        string currency = "GBP")
    {
        return new ComputeCapitalBalanceCommand(
            PoolId: poolId ?? Guid.NewGuid(),
            InvestorIdentityId: investorIdentityId,
            Currency: currency,
            RequestedBy: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow);
    }

    private static CapitalRecord CreateRecord(
        Guid poolId,
        CapitalType capitalType,
        decimal amount,
        string currency = "GBP",
        CapitalStatus status = CapitalStatus.Registered,
        Guid? ownerIdentityId = null)
    {
        return new CapitalRecord(
            CapitalId: Guid.NewGuid(),
            CapitalType: capitalType,
            PoolId: poolId,
            OwnerIdentityId: ownerIdentityId ?? Guid.NewGuid(),
            ClusterId: Guid.NewGuid(),
            SubClusterId: Guid.NewGuid(),
            SPVId: Guid.NewGuid(),
            Amount: amount,
            Currency: currency,
            Status: status,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ComputePoolBalance_ReturnsCorrectTotals()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Commitment, 100_000m),
            CreateRecord(poolId, CapitalType.Contribution, 80_000m),
            CreateRecord(poolId, CapitalType.Reservation, 30_000m),
            CreateRecord(poolId, CapitalType.Allocation, 20_000m),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(100_000m, result.Snapshot.TotalCommittedCapital);
        Assert.Equal(80_000m, result.Snapshot.TotalContributedCapital);
        Assert.Equal(30_000m, result.Snapshot.TotalReservedCapital);
        Assert.Equal(20_000m, result.Snapshot.TotalAllocatedCapital);
        Assert.Equal(poolId, result.Snapshot.PoolId);
        Assert.Equal("GBP", result.Snapshot.Currency);
    }

    [Fact]
    public void ComputeInvestorBalance_FiltersToSpecificInvestor()
    {
        var poolId = Guid.NewGuid();
        var investorId = Guid.NewGuid();
        var otherInvestorId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, investorIdentityId: investorId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Contribution, 50_000m, ownerIdentityId: investorId),
            CreateRecord(poolId, CapitalType.Contribution, 30_000m, ownerIdentityId: otherInvestorId),
            CreateRecord(poolId, CapitalType.Commitment, 60_000m, ownerIdentityId: investorId),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(50_000m, result.Snapshot.TotalContributedCapital);
        Assert.Equal(60_000m, result.Snapshot.TotalCommittedCapital);
        Assert.Equal(investorId, result.Snapshot.InvestorIdentityId);
    }

    [Fact]
    public void ComputeAvailableCapital_CalculatesCorrectly()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Contribution, 100_000m),
            CreateRecord(poolId, CapitalType.Reservation, 20_000m),
            CreateRecord(poolId, CapitalType.Allocation, 15_000m),
            CreateRecord(poolId, CapitalType.Allocation, 10_000m, status: CapitalStatus.Utilized),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);

        // Available = Contributed(100k) - Reserved(20k) - Allocated(25k) - Utilized(10k)
        Assert.Equal(100_000m - 20_000m - 25_000m - 10_000m, result.Snapshot.AvailableCapital);
    }

    [Fact]
    public void ValidateBalanceAggregation_ExcludesClosedRecords()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Contribution, 50_000m),
            CreateRecord(poolId, CapitalType.Contribution, 30_000m, status: CapitalStatus.Closed),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(50_000m, result.Snapshot.TotalContributedCapital);
    }

    [Fact]
    public void ValidateBalanceAggregation_ExcludesOtherCurrencies()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, currency: "GBP");

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Contribution, 50_000m, currency: "GBP"),
            CreateRecord(poolId, CapitalType.Contribution, 30_000m, currency: "USD"),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(50_000m, result.Snapshot.TotalContributedCapital);
    }

    [Fact]
    public async Task ConcurrentBalanceCalculations_ProduceDeterministicResults()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Commitment, 100_000m),
            CreateRecord(poolId, CapitalType.Contribution, 80_000m),
            CreateRecord(poolId, CapitalType.Reservation, 30_000m),
        };

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _engine.ComputeBalance(command, records)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.NotNull(r.Snapshot);
            Assert.Equal(100_000m, r.Snapshot.TotalCommittedCapital);
            Assert.Equal(80_000m, r.Snapshot.TotalContributedCapital);
            Assert.Equal(30_000m, r.Snapshot.TotalReservedCapital);
        });
    }

    [Fact]
    public void ComputeBalance_ReturnsError_WhenPoolIdEmpty()
    {
        var command = CreateCommand(poolId: Guid.Empty);

        var result = _engine.ComputeBalance(command, []);

        Assert.False(result.Success);
        Assert.Equal("PoolId must not be empty", result.Error);
    }

    [Fact]
    public void ComputeBalance_ReturnsError_WhenCurrencyEmpty()
    {
        var command = new ComputeCapitalBalanceCommand(
            Guid.NewGuid(), null, "", Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ComputeBalance(command, []);

        Assert.False(result.Success);
        Assert.Equal("Currency must not be empty", result.Error);
    }

    [Fact]
    public void ComputeBalance_ReturnsError_WhenUnsupportedCurrency()
    {
        var command = CreateCommand(currency: "BTC");

        var result = _engine.ComputeBalance(command, []);

        Assert.False(result.Success);
        Assert.Contains("Unsupported currency", result.Error);
    }

    [Fact]
    public void ComputeBalance_ReturnsZeros_WhenNoRecords()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var result = _engine.ComputeBalance(command, []);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(0m, result.Snapshot.TotalCommittedCapital);
        Assert.Equal(0m, result.Snapshot.TotalContributedCapital);
        Assert.Equal(0m, result.Snapshot.TotalReservedCapital);
        Assert.Equal(0m, result.Snapshot.TotalAllocatedCapital);
        Assert.Equal(0m, result.Snapshot.TotalUtilizedCapital);
        Assert.Equal(0m, result.Snapshot.TotalDistributedCapital);
        Assert.Equal(0m, result.Snapshot.AvailableCapital);
    }

    [Fact]
    public void ComputeBalance_IncludesDistributedCapital()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId);

        var records = new List<CapitalRecord>
        {
            CreateRecord(poolId, CapitalType.Contribution, 100_000m),
            CreateRecord(poolId, CapitalType.Distribution, 25_000m),
        };

        var result = _engine.ComputeBalance(command, records);

        Assert.True(result.Success);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(25_000m, result.Snapshot.TotalDistributedCapital);
    }
}
