namespace Whycespace.Systems.Downstream.Spv.Capital;

public sealed class SpvCapitalStructure
{
    private readonly Dictionary<Guid, List<InvestorAllocationModel>> _allocations = new();

    public void AddAllocation(Guid spvId, InvestorAllocationModel allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        if (!_allocations.TryGetValue(spvId, out var list))
        {
            list = new List<InvestorAllocationModel>();
            _allocations[spvId] = list;
        }

        var totalPercentage = list.Sum(a => a.AllocationPercentage) + allocation.AllocationPercentage;
        if (totalPercentage > 100m)
            throw new InvalidOperationException("Total allocation percentage cannot exceed 100%.");

        list.Add(allocation);
    }

    public IReadOnlyList<InvestorAllocationModel> GetAllocations(Guid spvId)
    {
        if (!_allocations.TryGetValue(spvId, out var list))
            return [];

        return list;
    }

    public decimal GetRemainingCapacity(Guid spvId)
    {
        if (!_allocations.TryGetValue(spvId, out var list))
            return 100m;

        return 100m - list.Sum(a => a.AllocationPercentage);
    }
}
