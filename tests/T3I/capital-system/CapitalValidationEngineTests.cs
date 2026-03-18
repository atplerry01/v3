namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Engines.T3I.Monitoring.Economic;

public sealed class CapitalValidationEngineTests
{
    private readonly CapitalValidationEngine _engine = new();

    private static ValidateCapitalOperationCommand CreateCommand(
        CapitalOperationType operationType = CapitalOperationType.Contribution,
        Guid? poolId = null,
        Guid? reservationId = null,
        Guid? allocationId = null,
        decimal amount = 10_000m,
        string currency = "GBP")
    {
        return new ValidateCapitalOperationCommand(
            OperationType: operationType,
            PoolId: poolId ?? Guid.NewGuid(),
            ReservationId: reservationId,
            AllocationId: allocationId,
            InvestorIdentityId: Guid.NewGuid(),
            Amount: amount,
            Currency: currency,
            RequestedBy: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow);
    }

    private static CapitalPoolSnapshot ActivePool(
        Guid? poolId = null,
        decimal availableBalance = 100_000m,
        string currency = "GBP")
    {
        return new CapitalPoolSnapshot(
            PoolId: poolId ?? Guid.NewGuid(),
            IsActive: true,
            TotalBalance: availableBalance,
            AvailableBalance: availableBalance,
            Currency: currency);
    }

    private static InvestorSnapshot AuthorizedInvestor(
        decimal maxContribution = 50_000m,
        decimal totalContributed = 0m)
    {
        return new InvestorSnapshot(
            InvestorIdentityId: Guid.NewGuid(),
            IsAuthorized: true,
            MaxContribution: maxContribution,
            TotalContributed: totalContributed);
    }

    [Fact]
    public void ValidatePoolExists_ReturnsError_WhenPoolNull()
    {
        var command = CreateCommand();

        var result = _engine.Validate(command, pool: null, reservation: null, allocation: null, investor: AuthorizedInvestor());

        Assert.False(result.IsValid);
        Assert.Contains("POOL_NOT_FOUND", result.ValidationErrors);
    }

    [Fact]
    public void ValidatePoolExists_ReturnsError_WhenPoolInactive()
    {
        var pool = new CapitalPoolSnapshot(Guid.NewGuid(), IsActive: false, 100_000m, 100_000m, "GBP");
        var command = CreateCommand();

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: AuthorizedInvestor());

        Assert.False(result.IsValid);
        Assert.Contains("POOL_NOT_ACTIVE", result.ValidationErrors);
    }

    [Fact]
    public void ValidateReservationIntegrity_ReturnsError_WhenReservationMissing()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Allocation);
        var pool = ActivePool();

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: null);

        Assert.False(result.IsValid);
        Assert.Contains("INVALID_RESERVATION", result.ValidationErrors);
    }

    [Fact]
    public void ValidateReservationIntegrity_ReturnsError_WhenReservationInactive()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Allocation, amount: 5_000m);
        var pool = ActivePool();
        var reservation = new CapitalReservationSnapshot(Guid.NewGuid(), pool.PoolId, IsActive: false, 10_000m, 0m);

        var result = _engine.Validate(command, pool, reservation, allocation: null, investor: null);

        Assert.False(result.IsValid);
        Assert.Contains("RESERVATION_NOT_ACTIVE", result.ValidationErrors);
    }

    [Fact]
    public void ValidateAllocationIntegrity_ReturnsError_WhenAllocationMissing()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Utilization);
        var pool = ActivePool();

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: null);

        Assert.False(result.IsValid);
        Assert.Contains("INVALID_ALLOCATION_REFERENCE", result.ValidationErrors);
    }

    [Fact]
    public void ValidateAllocationIntegrity_ReturnsError_WhenAllocationInactive()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Utilization, amount: 5_000m);
        var pool = ActivePool();
        var allocation = new CapitalAllocationSnapshot(Guid.NewGuid(), Guid.NewGuid(), IsActive: false, 10_000m, 0m);

        var result = _engine.Validate(command, pool, reservation: null, allocation, investor: null);

        Assert.False(result.IsValid);
        Assert.Contains("INVALID_LIFECYCLE_STATE", result.ValidationErrors);
    }

    [Fact]
    public void ValidateInvestorContributionLimits_ReturnsError_WhenLimitExceeded()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Contribution, amount: 30_000m);
        var pool = ActivePool();
        var investor = AuthorizedInvestor(maxContribution: 50_000m, totalContributed: 40_000m);

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor);

        Assert.False(result.IsValid);
        Assert.Contains("INVESTOR_LIMIT_EXCEEDED", result.ValidationErrors);
    }

    [Fact]
    public void ValidateInvestorContributionLimits_ReturnsError_WhenNotAuthorized()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Contribution);
        var pool = ActivePool();
        var investor = new InvestorSnapshot(Guid.NewGuid(), IsAuthorized: false, 50_000m, 0m);

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor);

        Assert.False(result.IsValid);
        Assert.Contains("INVESTOR_NOT_AUTHORIZED", result.ValidationErrors);
    }

    [Fact]
    public void ValidateCurrencyConsistency_ReturnsError_WhenCurrencyMismatch()
    {
        var command = CreateCommand(currency: "USD");
        var pool = ActivePool(currency: "GBP");

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: AuthorizedInvestor());

        Assert.False(result.IsValid);
        Assert.Contains("CURRENCY_MISMATCH", result.ValidationErrors);
    }

    [Fact]
    public void ValidateCurrencyConsistency_ReturnsError_WhenUnsupportedCurrency()
    {
        var command = CreateCommand(currency: "BTC");
        var pool = ActivePool(currency: "BTC");

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: AuthorizedInvestor());

        Assert.False(result.IsValid);
        Assert.Contains("UNSUPPORTED_CURRENCY", result.ValidationErrors);
    }

    [Fact]
    public void ValidateContribution_ReturnsValid_WhenAllRulesPass()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Contribution, amount: 10_000m);
        var pool = ActivePool();
        var investor = AuthorizedInvestor();

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor);

        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationErrors);
        Assert.Equal(command.PoolId, result.PoolId);
        Assert.Equal(command.Amount, result.Amount);
        Assert.Equal(command.Currency, result.Currency);
    }

    [Fact]
    public void ValidateReservation_ReturnsError_WhenInsufficientPoolCapital()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Reservation, amount: 200_000m);
        var pool = ActivePool(availableBalance: 100_000m);

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor: null);

        Assert.False(result.IsValid);
        Assert.Contains("INSUFFICIENT_POOL_CAPITAL", result.ValidationErrors);
    }

    [Fact]
    public async Task ConcurrentValidationRequests_ProduceDeterministicResults()
    {
        var command = CreateCommand(operationType: CapitalOperationType.Contribution, amount: 10_000m);
        var pool = ActivePool();
        var investor = AuthorizedInvestor();

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _engine.Validate(command, pool, reservation: null, allocation: null, investor)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.IsValid);
            Assert.Empty(r.ValidationErrors);
        });
    }

    [Fact]
    public void ValidateAmount_ReturnsError_WhenZeroOrNegative()
    {
        var command = CreateCommand(amount: 0m);
        var pool = ActivePool();
        var investor = AuthorizedInvestor();

        var result = _engine.Validate(command, pool, reservation: null, allocation: null, investor);

        Assert.False(result.IsValid);
        Assert.Contains("INVALID_AMOUNT", result.ValidationErrors);
    }
}
