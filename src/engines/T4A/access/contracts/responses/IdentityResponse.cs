namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record IdentityResponse(
    string IdentityId,
    string DisplayName,
    string Email,
    string IdentityType,
    string Status);
