namespace Whycespace.Systems.WhyceID.Commands;

public sealed record UpdateIdentityAttributeCommand(
    Guid IdentityId,
    string AttributeKey,
    string AttributeValue,
    Guid RequestedBy,
    DateTime Timestamp);
