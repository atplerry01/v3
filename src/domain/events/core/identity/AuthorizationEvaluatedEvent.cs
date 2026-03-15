namespace Whycespace.Domain.Events.Core.Identity;

public sealed record AuthorizationEvaluatedEvent(
    Guid EventId,
    Guid IdentityId,
    string ResourceType,
    string Action,
    bool Authorized,
    DateTime EvaluatedAt,
    int EventVersion);
