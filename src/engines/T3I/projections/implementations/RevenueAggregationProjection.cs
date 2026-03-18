using Whycespace.Domain.Events.Core.Economic;
using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Implementations;

public sealed class RevenueAggregationProjection : IProjection
{
    private readonly AtlasProjectionStore<RevenueAggregationModel> _store;

    public RevenueAggregationProjection(AtlasProjectionStore<RevenueAggregationModel> store)
    {
        _store = store;
    }

    public string Name => "AtlasRevenueAggregation";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "whyce.economic.revenue-recorded",
        "whyce.economic.profit-distributed"
    ];

    public Task HandleAsync(EventEnvelope envelope)
    {
        if (_store.HasProcessed(envelope.EventId))
            return Task.CompletedTask;

        switch (envelope.Payload)
        {
            case RevenueRecordedEvent e:
                ApplyRevenue(e);
                break;

            case ProfitDistributedEvent e:
                ApplyProfitDistribution(e);
                break;
        }

        _store.MarkProcessed(envelope.EventId);
        return Task.CompletedTask;
    }

    public AtlasProjectionStore<RevenueAggregationModel> Store => _store;

    private void ApplyRevenue(RevenueRecordedEvent e)
    {
        var current = _store.Get(e.SpvId) ?? NewRevenue(e.SpvId);
        _store.Upsert(e.SpvId, current with
        {
            TotalRevenue = current.TotalRevenue + e.Amount,
            UndistributedRevenue = current.UndistributedRevenue + e.Amount,
            RevenueEventCount = current.RevenueEventCount + 1,
            LastUpdatedAt = e.Timestamp
        });
    }

    private void ApplyProfitDistribution(ProfitDistributedEvent e)
    {
        var current = _store.Get(e.SpvId) ?? NewRevenue(e.SpvId);
        _store.Upsert(e.SpvId, current with
        {
            TotalProfitDistributed = current.TotalProfitDistributed + e.Amount,
            UndistributedRevenue = current.UndistributedRevenue - e.Amount,
            LastUpdatedAt = e.Timestamp
        });
    }

    private static RevenueAggregationModel NewRevenue(Guid spvId) =>
        new(spvId, 0m, 0m, 0m, 0, DateTimeOffset.UtcNow);
}
