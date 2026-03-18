namespace Whycespace.Systems.Downstream.Cwg.Vaults.Participants;

public interface IVaultParticipantRegistry
{
    void RegisterParticipant(VaultParticipantRegistryRecord record);
    VaultParticipantRegistryRecord? GetParticipant(Guid participantId);
    IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByVault(Guid vaultId);
    IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByIdentity(Guid identityId);
    IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByRole(string role);
    IReadOnlyList<VaultParticipantRegistryRecord> ListParticipants();
}
