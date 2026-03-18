namespace Whycespace.Domain.Economic.Capital;

public interface ICapitalRegistryStore
{
    void SaveCapitalRecord(CapitalRecord record);
    CapitalRecord? GetCapitalRecord(Guid capitalId);
    IReadOnlyList<CapitalRecord> ListCapitalByPool(Guid poolId);
    IReadOnlyList<CapitalRecord> ListCapitalByOwner(Guid ownerIdentityId);
    IReadOnlyList<CapitalRecord> ListCapitalBySPV(Guid spvId);
}
