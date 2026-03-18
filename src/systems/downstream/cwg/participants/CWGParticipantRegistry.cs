namespace Whycespace.Systems.Downstream.Cwg.Participants;

public sealed class CWGParticipantRegistry
{
    private readonly Dictionary<Guid, CWGParticipantRecord> _participants = new();
    private readonly Dictionary<string, List<Guid>> _roleIndex = new();
    private readonly Dictionary<string, List<Guid>> _clusterIndex = new();

    public void Register(CWGParticipantRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_participants.ContainsKey(record.ParticipantId))
            throw new InvalidOperationException($"Participant '{record.ParticipantId}' is already registered.");

        _participants[record.ParticipantId] = record;

        if (!_roleIndex.TryGetValue(record.Role, out var roleList))
        {
            roleList = new List<Guid>();
            _roleIndex[record.Role] = roleList;
        }
        roleList.Add(record.ParticipantId);

        if (!_clusterIndex.TryGetValue(record.ClusterId, out var clusterList))
        {
            clusterList = new List<Guid>();
            _clusterIndex[record.ClusterId] = clusterList;
        }
        clusterList.Add(record.ParticipantId);
    }

    public CWGParticipantRecord? Get(Guid participantId)
    {
        _participants.TryGetValue(participantId, out var record);
        return record;
    }

    public IReadOnlyList<CWGParticipantRecord> GetByRole(string role)
    {
        if (!_roleIndex.TryGetValue(role, out var ids))
            return [];

        return ids.Select(id => _participants[id]).ToList();
    }

    public IReadOnlyList<CWGParticipantRecord> GetByCluster(string clusterId)
    {
        if (!_clusterIndex.TryGetValue(clusterId, out var ids))
            return [];

        return ids.Select(id => _participants[id]).ToList();
    }

    public IReadOnlyList<CWGParticipantRecord> ListAll() => _participants.Values.ToList();
}
