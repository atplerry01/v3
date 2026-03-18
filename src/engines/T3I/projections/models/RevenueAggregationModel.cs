namespace Whycespace.Engines.T3I.Projections.Models;

public sealed record RevenueAggregationModel(
    Guid SpvId,
    decimal TotalRevenue,
    decimal TotalProfitDistributed,
    decimal UndistributedRevenue,
    int RevenueEventCount,
    DateTimeOffset LastUpdatedAt);
