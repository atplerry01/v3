namespace Whycespace.Engines.T0U.WhyceID.Models;

public sealed record IdentityAttributeUpdateResult(
    Guid IdentityId,
    string AttributeKey,
    string AttributeValue,
    DateTime UpdatedAt,
    Guid RequestedBy);
