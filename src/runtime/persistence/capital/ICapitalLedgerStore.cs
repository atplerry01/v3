namespace Whycespace.Infrastructure.Persistence.CapitalLedger;

public interface ICapitalLedgerStore
{
    void AppendEntry(CapitalLedgerEntry entry);
    IReadOnlyList<CapitalLedgerEntry> GetEntriesByCapitalId(Guid capitalId);
    IReadOnlyList<CapitalLedgerEntry> GetEntriesByPoolId(Guid poolId);
    IReadOnlyList<CapitalLedgerEntry> GetEntriesByInvestor(Guid investorIdentityId);
    IReadOnlyList<CapitalLedgerEntry> GetEntriesByReferenceId(Guid referenceId);
    IReadOnlyList<CapitalLedgerEntry> GetLedgerRange(DateTimeOffset startDate, DateTimeOffset endDate);
}
