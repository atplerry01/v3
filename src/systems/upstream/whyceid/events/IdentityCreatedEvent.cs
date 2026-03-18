namespace Whycespace.Systems.WhyceID.Events;

using Whycespace.Systems.WhyceID.Models;

public sealed record IdentityCreatedEvent(
    Guid IdentityId,
    IdentityType Type,
    DateTime CreatedAt);
