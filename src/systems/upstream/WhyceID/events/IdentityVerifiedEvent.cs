namespace Whycespace.Systems.WhyceID.Events;

public sealed record IdentityVerifiedEvent(
    Guid IdentityId,
    DateTime VerifiedAt);
