namespace Whycespace.Engines.T0U.WhyceID.Identity.Attributes;

public sealed record IdentityAttributeUpdateResult(
    Guid IdentityId,
    string AttributeKey,
    string AttributeValue,
    DateTime UpdatedAt,
    Guid RequestedBy);
