namespace Whycespace.Systems.Midstream.WhycePlus.Optimization;

public sealed class AllocationOptimizer
{
    public AllocationRecommendation OptimizeAllocation(
        string poolId,
        IReadOnlyList<AllocationEntry> currentAllocations,
        decimal availableCapital)
    {
        var totalAllocated = currentAllocations.Sum(a => a.Amount);
        var remainingCapital = availableCapital - totalAllocated;
        var utilizationRatio = availableCapital > 0 ? totalAllocated / availableCapital : 0m;

        return new AllocationRecommendation(
            poolId, totalAllocated, remainingCapital, utilizationRatio,
            utilizationRatio > 0.9m ? "Over-allocated" : utilizationRatio < 0.3m ? "Under-utilized" : "Balanced",
            DateTimeOffset.UtcNow);
    }
}

public sealed record AllocationEntry(string AllocationId, string RecipientId, decimal Amount);

public sealed record AllocationRecommendation(
    string PoolId,
    decimal TotalAllocated,
    decimal RemainingCapital,
    decimal UtilizationRatio,
    string Assessment,
    DateTimeOffset EvaluatedAt
);
