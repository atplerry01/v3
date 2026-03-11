namespace Whycespace.System.WhyceID.Aggregates;

using Whycespace.System.WhyceID.Models;

public sealed class IdentityAggregate
{
    public IdentityId IdentityId { get; }

    public IdentityType Type { get; }

    public IdentityStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public IdentityAggregate(
        IdentityId identityId,
        IdentityType type,
        DateTime createdAt)
    {
        IdentityId = identityId;
        Type = type;
        CreatedAt = createdAt;
        Status = IdentityStatus.PendingVerification;
    }

    public void Activate()
    {
        if (Status != IdentityStatus.PendingVerification)
            throw new InvalidOperationException("Identity cannot be activated");

        Status = IdentityStatus.Active;
    }

    public void Suspend()
    {
        if (Status != IdentityStatus.Active)
            throw new InvalidOperationException("Only active identity may be suspended");

        Status = IdentityStatus.Suspended;
    }
}
