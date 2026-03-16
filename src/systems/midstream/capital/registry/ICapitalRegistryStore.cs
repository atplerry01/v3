namespace Whycespace.Systems.Midstream.Capital.Registry;

public interface ICapitalRegistryStore
{
    void SaveCapitalRecord(CapitalRecord record);
    CapitalRecord? GetCapitalRecord(Guid capitalId);
    IReadOnlyList<CapitalRecord> ListCapitalByPool(Guid poolId);
    IReadOnlyList<CapitalRecord> ListCapitalByOwner(Guid ownerIdentityId);
    IReadOnlyList<CapitalRecord> ListCapitalBySPV(Guid spvId);
}
