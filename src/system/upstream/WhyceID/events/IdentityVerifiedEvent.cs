namespace Whycespace.System.WhyceID.Events;

public sealed record IdentityVerifiedEvent(
    Guid IdentityId,
    DateTime VerifiedAt);
