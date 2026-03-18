namespace Whycespace.Domain.Identity;

public readonly record struct ParticipantId(Guid Value)
{
    public static ParticipantId New() => new(Guid.NewGuid());
    public static ParticipantId Empty => new(Guid.Empty);
    public static implicit operator Guid(ParticipantId id) => id.Value;
    public static implicit operator ParticipantId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
