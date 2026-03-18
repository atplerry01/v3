namespace Whycespace.Infrastructure.Persistence.CapitalLedger;

public enum LedgerEntryType
{
    CommitmentRecorded,
    ContributionRecorded,
    ReservationRecorded,
    AllocationRecorded,
    UtilizationRecorded,
    DistributionRecorded,
    DistributionReversal
}

public sealed record CapitalLedgerEntry(
    Guid EntryId,
    LedgerEntryType EntryType,
    Guid CapitalId,
    Guid PoolId,
    Guid InvestorIdentityId,
    Guid ReferenceId,
    decimal Amount,
    string Currency,
    decimal PreviousBalance,
    decimal NewBalance,
    DateTimeOffset Timestamp,
    string TraceId,
    string CorrelationId
);
