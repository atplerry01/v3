using Whycespace.Domain.Economic.Events;
using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Implementations;

public sealed class CapitalBalanceProjection : IProjection
{
    private readonly AtlasProjectionStore<CapitalBalanceModel> _store;

    public CapitalBalanceProjection(AtlasProjectionStore<CapitalBalanceModel> store)
    {
        _store = store;
    }

    public string Name => "AtlasCapitalBalance";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "whyce.economic.capital-contribution-recorded",
        "whyce.economic.capital-distributed",
        "whyce.economic.capital-reserved",
        "whyce.economic.capital-reservation-released",
        "whyce.economic.capital-reservation-expired"
    ];

    public Task HandleAsync(EventEnvelope envelope)
    {
        if (_store.HasProcessed(envelope.EventId))
            return Task.CompletedTask;

        switch (envelope.Payload)
        {
            case CapitalContributionRecordedEvent e:
                ApplyContribution(e);
                break;

            case CapitalDistributedEvent e:
                ApplyDistribution(e);
                break;

            case CapitalReservedEvent e:
                ApplyReservation(e);
                break;

            case CapitalReservationReleasedEvent e:
                ApplyReservationReleased(e);
                break;

            case CapitalReservationExpiredEvent e:
                ApplyReservationExpired(e);
                break;
        }

        _store.MarkProcessed(envelope.EventId);
        return Task.CompletedTask;
    }

    public AtlasProjectionStore<CapitalBalanceModel> Store => _store;

    private void ApplyContribution(CapitalContributionRecordedEvent e)
    {
        var current = _store.Get(e.SpvId) ?? NewBalance(e.SpvId);
        _store.Upsert(e.SpvId, current with
        {
            TotalContributions = current.TotalContributions + e.Amount,
            NetBalance = current.NetBalance + e.Amount,
            TransactionCount = current.TransactionCount + 1,
            LastUpdatedAt = e.Timestamp
        });
    }

    private void ApplyDistribution(CapitalDistributedEvent e)
    {
        var current = _store.Get(e.PoolId) ?? NewBalance(e.PoolId);
        _store.Upsert(e.PoolId, current with
        {
            TotalDistributions = current.TotalDistributions + e.TotalAmount,
            NetBalance = current.NetBalance - e.TotalAmount,
            TransactionCount = current.TransactionCount + 1,
            LastUpdatedAt = e.Timestamp
        });
    }

    private void ApplyReservation(CapitalReservedEvent e)
    {
        var current = _store.Get(e.PoolId) ?? NewBalance(e.PoolId);
        _store.Upsert(e.PoolId, current with
        {
            TotalReserved = current.TotalReserved + e.Amount,
            NetBalance = current.NetBalance - e.Amount,
            TransactionCount = current.TransactionCount + 1,
            LastUpdatedAt = e.Timestamp
        });
    }

    private void ApplyReservationReleased(CapitalReservationReleasedEvent e)
    {
        // Reservation released — cannot determine amount from event alone.
        // In production, this would look up the reservation amount.
        // For projection correctness, we track the event occurrence.
    }

    private void ApplyReservationExpired(CapitalReservationExpiredEvent e)
    {
        // Reservation expired — same constraint as released.
    }

    private static CapitalBalanceModel NewBalance(Guid spvId) =>
        new(spvId, 0m, 0m, 0m, 0m, 0, DateTimeOffset.UtcNow);
}
