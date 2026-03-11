namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityAttribute(
    string Key,
    string Value,
    DateTime CreatedAt);
