namespace Whycespace.Systems.WhyceID.Aggregates;

using Whycespace.Systems.WhyceID.Models;

public sealed class IdentityAggregate
{
    public IdentityId Id { get; }

    public IdentityStatus Status { get; private set; }

    public IdentityType Type { get; }

    public DateTime CreatedAt { get; }

    public DateTime? VerifiedAt { get; private set; }

    public IdentityAggregate(IdentityId id, IdentityType type)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Type = type;
        Status = IdentityStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        if (Status != IdentityStatus.Pending)
            throw new InvalidOperationException("Only pending identities can be verified.");

        Status = IdentityStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == IdentityStatus.Revoked)
            throw new InvalidOperationException("Revoked identities cannot be suspended.");

        Status = IdentityStatus.Suspended;
    }

    public void Revoke()
    {
        Status = IdentityStatus.Revoked;
    }
}
