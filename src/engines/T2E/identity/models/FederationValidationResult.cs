namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record FederationValidationResult(
    string ProviderName,
    string ExternalIdentityId,
    bool Valid,
    DateTime ValidatedAt);
