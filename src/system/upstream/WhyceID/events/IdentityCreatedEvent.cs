namespace Whycespace.System.WhyceID.Events;

using Whycespace.System.WhyceID.Models;

public sealed record IdentityCreatedEvent(
    Guid IdentityId,
    IdentityType Type,
    DateTime CreatedAt);
