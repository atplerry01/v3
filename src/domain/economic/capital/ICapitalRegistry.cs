namespace Whycespace.Domain.Core.Economic;

public interface ICapitalRegistry
{
    void RegisterCapital(CapitalRecord record);
    CapitalRecord? GetCapital(Guid capitalId);
    IReadOnlyList<CapitalRecord> ListCapitalByPool(Guid poolId);
    IReadOnlyList<CapitalRecord> ListCapitalByOwner(Guid ownerIdentityId);
    IReadOnlyList<CapitalRecord> ListCapitalBySPV(Guid spvId);
    void UpdateCapitalStatus(Guid capitalId, CapitalStatus newStatus);
}
