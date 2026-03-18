namespace Whycespace.Domain.Identity.Events;

public sealed record AuthorizationEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    string ResourceType,
    string Action,
    bool Authorized,
    DateTime EvaluatedAt,
    int EventVersion);
