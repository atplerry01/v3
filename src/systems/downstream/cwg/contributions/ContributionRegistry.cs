namespace Whycespace.Systems.Downstream.Cwg.Contributions;

public sealed class ContributionRegistry
{
    private readonly Dictionary<Guid, ContributionRecord> _contributions = new();
    private readonly Dictionary<Guid, List<Guid>> _participantIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();

    public void Register(ContributionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_contributions.ContainsKey(record.ContributionId))
            throw new InvalidOperationException($"Contribution '{record.ContributionId}' is already registered.");

        _contributions[record.ContributionId] = record;

        if (!_participantIndex.TryGetValue(record.ParticipantId, out var pList))
        {
            pList = new List<Guid>();
            _participantIndex[record.ParticipantId] = pList;
        }
        pList.Add(record.ContributionId);

        if (!_vaultIndex.TryGetValue(record.VaultId, out var vList))
        {
            vList = new List<Guid>();
            _vaultIndex[record.VaultId] = vList;
        }
        vList.Add(record.ContributionId);
    }

    public ContributionRecord? Get(Guid contributionId)
    {
        _contributions.TryGetValue(contributionId, out var record);
        return record;
    }

    public IReadOnlyList<ContributionRecord> GetByParticipant(Guid participantId)
    {
        if (!_participantIndex.TryGetValue(participantId, out var ids))
            return [];

        return ids.Select(id => _contributions[id]).ToList();
    }

    public IReadOnlyList<ContributionRecord> GetByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _contributions[id]).ToList();
    }

    public IReadOnlyList<ContributionRecord> ListAll() => _contributions.Values.ToList();
}
