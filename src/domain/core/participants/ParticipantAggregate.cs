namespace Whycespace.Domain.Core.Participants;

using Whycespace.Domain.Core.Participants.Events;

public sealed class ParticipantAggregate
{
    private readonly List<ParticipantRole> _roles = new();
    private readonly List<object> _domainEvents = new();

    public ParticipantId ParticipantId { get; }
    public ParticipantProfile Profile { get; }
    public ParticipantStatus Status { get; private set; }
    public IReadOnlyList<ParticipantRole> Roles => _roles.AsReadOnly();
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private ParticipantAggregate(
        ParticipantId participantId,
        ParticipantProfile profile,
        ParticipantRole initialRole)
    {
        ParticipantId = participantId;
        Profile = profile;
        Status = ParticipantStatus.PendingRegistration;
        _roles.Add(initialRole);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static ParticipantAggregate RegisterParticipant(
        ParticipantProfile profile,
        ParticipantRole initialRole = ParticipantRole.Participant)
    {
        if (profile.ParticipantId == ParticipantId.Empty)
            throw new InvalidOperationException("ParticipantId must exist.");

        var aggregate = new ParticipantAggregate(profile.ParticipantId, profile, initialRole);

        aggregate._domainEvents.Add(
            ParticipantRegisteredEvent.Create(profile.ParticipantId, profile.Email));

        return aggregate;
    }

    public void AssignRole(ParticipantRole role)
    {
        if (Status == ParticipantStatus.Suspended)
            throw new InvalidOperationException("Suspended participants cannot receive new roles.");

        if (_roles.Contains(role))
            throw new InvalidOperationException($"Participant already has role '{role}'.");

        _roles.Add(role);
        UpdatedAt = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            ParticipantRoleAssignedEvent.Create(ParticipantId, role));
    }

    public void RemoveRole(ParticipantRole role)
    {
        if (!_roles.Contains(role))
            throw new InvalidOperationException($"Participant does not have role '{role}'.");

        if (_roles.Count == 1)
            throw new InvalidOperationException("Participant must have at least one role.");

        _roles.Remove(role);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == ParticipantStatus.Active)
            throw new InvalidOperationException("Participant is already active.");

        Status = ParticipantStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            ParticipantActivatedEvent.Create(ParticipantId));
    }

    public void Suspend()
    {
        if (Status != ParticipantStatus.Active)
            throw new InvalidOperationException("Only active participants can be suspended.");

        Status = ParticipantStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            ParticipantSuspendedEvent.Create(ParticipantId));
    }

    public void Deactivate()
    {
        if (Status == ParticipantStatus.Disabled)
            throw new InvalidOperationException("Participant is already disabled.");

        Status = ParticipantStatus.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
