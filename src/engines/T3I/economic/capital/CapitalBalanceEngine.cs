namespace Whycespace.Engines.T3I.Economic.Capital;

using Whycespace.Domain.Core.Economic.CapitalRegistry;

public sealed class CapitalBalanceEngine
{
    private static readonly string[] SupportedCurrencies = ["GBP", "USD", "EUR", "NGN"];

    public CapitalBalanceResult ComputeBalance(
        ComputeCapitalBalanceCommand command,
        IReadOnlyList<CapitalRecord> capitalRecords)
    {
        var validationError = Validate(command);
        if (validationError is not null)
            return CapitalBalanceResult.Fail(validationError);

        var filtered = FilterRecords(capitalRecords, command);

        var committed = AggregateByType(filtered, CapitalType.Commitment);
        var contributed = AggregateByType(filtered, CapitalType.Contribution);
        var reserved = AggregateByType(filtered, CapitalType.Reservation);
        var allocated = AggregateByType(filtered, CapitalType.Allocation);
        var utilized = AggregateByType(filtered, CapitalType.Distribution) is var _ ? AggregateUtilized(filtered) : 0m;
        var distributed = AggregateByType(filtered, CapitalType.Distribution);

        utilized = AggregateUtilized(filtered);

        var available = contributed - reserved - allocated - utilized;

        var snapshot = new CapitalBalanceSnapshot(
            PoolId: command.PoolId,
            InvestorIdentityId: command.InvestorIdentityId,
            Currency: command.Currency,
            TotalCommittedCapital: committed,
            TotalContributedCapital: contributed,
            TotalReservedCapital: reserved,
            TotalAllocatedCapital: allocated,
            TotalUtilizedCapital: utilized,
            TotalDistributedCapital: distributed,
            AvailableCapital: available,
            Timestamp: DateTime.UtcNow);

        return CapitalBalanceResult.Ok(snapshot);
    }

    private static IReadOnlyList<CapitalRecord> FilterRecords(
        IReadOnlyList<CapitalRecord> records,
        ComputeCapitalBalanceCommand command)
    {
        var filtered = records
            .Where(r => r.PoolId == command.PoolId
                && r.Currency == command.Currency
                && r.Status != CapitalStatus.Closed);

        if (command.InvestorIdentityId.HasValue)
        {
            filtered = filtered.Where(r => r.OwnerIdentityId == command.InvestorIdentityId.Value);
        }

        return filtered.ToList();
    }

    private static decimal AggregateByType(IReadOnlyList<CapitalRecord> records, CapitalType type)
    {
        return records
            .Where(r => r.CapitalType == type)
            .Sum(r => r.Amount);
    }

    private static decimal AggregateUtilized(IReadOnlyList<CapitalRecord> records)
    {
        return records
            .Where(r => r.Status == CapitalStatus.Utilized)
            .Sum(r => r.Amount);
    }

    private static string? Validate(ComputeCapitalBalanceCommand command)
    {
        if (command.PoolId == Guid.Empty)
            return "PoolId must not be empty";

        if (string.IsNullOrWhiteSpace(command.Currency))
            return "Currency must not be empty";

        if (!Array.Exists(SupportedCurrencies, c => c == command.Currency))
            return $"Unsupported currency: {command.Currency}";

        if (command.RequestedBy == Guid.Empty)
            return "RequestedBy must not be empty";

        return null;
    }
}
