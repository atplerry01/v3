namespace Whycespace.Systems.Downstream.Cwg.Vaults.Participants;

public sealed class VaultParticipantRegistry : IVaultParticipantRegistry
{
    private readonly Dictionary<Guid, VaultParticipantRegistryRecord> _participants = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _identityIndex = new();
    private readonly Dictionary<string, List<Guid>> _roleIndex = new();

    public void RegisterParticipant(VaultParticipantRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required.", nameof(record));

        if (record.IdentityId == Guid.Empty)
            throw new ArgumentException("IdentityId is required.", nameof(record));

        if (_participants.ContainsKey(record.ParticipantId))
            throw new InvalidOperationException($"Participant '{record.ParticipantId}' is already registered.");

        _participants[record.ParticipantId] = record;

        if (!_vaultIndex.TryGetValue(record.VaultId, out var vaultList))
        {
            vaultList = new List<Guid>();
            _vaultIndex[record.VaultId] = vaultList;
        }
        vaultList.Add(record.ParticipantId);

        if (!_identityIndex.TryGetValue(record.IdentityId, out var identityList))
        {
            identityList = new List<Guid>();
            _identityIndex[record.IdentityId] = identityList;
        }
        identityList.Add(record.ParticipantId);

        if (!_roleIndex.TryGetValue(record.ParticipantRole, out var roleList))
        {
            roleList = new List<Guid>();
            _roleIndex[record.ParticipantRole] = roleList;
        }
        roleList.Add(record.ParticipantId);
    }

    public VaultParticipantRegistryRecord? GetParticipant(Guid participantId)
    {
        _participants.TryGetValue(participantId, out var record);
        return record;
    }

    public IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _participants[id]).ToList();
    }

    public IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByIdentity(Guid identityId)
    {
        if (!_identityIndex.TryGetValue(identityId, out var ids))
            return [];

        return ids.Select(id => _participants[id]).ToList();
    }

    public IReadOnlyList<VaultParticipantRegistryRecord> GetParticipantsByRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        if (!_roleIndex.TryGetValue(role, out var ids))
            return [];

        return ids.Select(id => _participants[id]).ToList();
    }

    public IReadOnlyList<VaultParticipantRegistryRecord> ListParticipants() => _participants.Values.ToList();
}
