
using Whycespace.Domain.Economic.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.Contracts.Events;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Implementations;

public sealed class VaultCashflowProjection : IProjection
{
    private readonly AtlasProjectionStore<VaultCashflowModel> _store;

    public VaultCashflowProjection(AtlasProjectionStore<VaultCashflowModel> store)
    {
        _store = store;
    }

    public string Name => "AtlasVaultCashflow";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "whyce.economic.capital-contribution-recorded",
        "whyce.economic.capital-distributed",
        "whyce.economic.revenue-recorded",
        "whyce.economic.profit-distributed"
    ];

    public Task HandleAsync(EventEnvelope envelope)
    {
        if (_store.HasProcessed(envelope.EventId))
            return Task.CompletedTask;

        switch (envelope.Payload)
        {
            case CapitalContributionRecordedEvent e:
                ApplyInflow(e.SpvId, e.Amount, e.Timestamp);
                break;

            case RevenueRecordedEvent e:
                ApplyInflow(e.SpvId, e.Amount, e.Timestamp);
                break;

            case CapitalDistributedEvent e:
                ApplyOutflow(e.PoolId, e.TotalAmount, e.Timestamp);
                break;

            case ProfitDistributedEvent e:
                ApplyOutflow(e.SpvId, e.Amount, e.Timestamp);
                break;
        }

        _store.MarkProcessed(envelope.EventId);
        return Task.CompletedTask;
    }

    public AtlasProjectionStore<VaultCashflowModel> Store => _store;

    private void ApplyInflow(Guid spvId, decimal amount, DateTimeOffset timestamp)
    {
        var current = _store.Get(spvId) ?? NewCashflow(spvId);
        _store.Upsert(spvId, current with
        {
            TotalInflows = current.TotalInflows + amount,
            NetCashflow = current.NetCashflow + amount,
            InflowCount = current.InflowCount + 1,
            LastUpdatedAt = timestamp
        });
    }

    private void ApplyOutflow(Guid spvId, decimal amount, DateTimeOffset timestamp)
    {
        var current = _store.Get(spvId) ?? NewCashflow(spvId);
        _store.Upsert(spvId, current with
        {
            TotalOutflows = current.TotalOutflows + amount,
            NetCashflow = current.NetCashflow - amount,
            OutflowCount = current.OutflowCount + 1,
            LastUpdatedAt = timestamp
        });
    }

    private static VaultCashflowModel NewCashflow(Guid spvId) =>
        new(spvId, 0m, 0m, 0m, 0, 0, DateTimeOffset.UtcNow);
}
