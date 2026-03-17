namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record FederationValidationResult(
    string ProviderName,
    string ExternalIdentityId,
    bool Valid,
    DateTime ValidatedAt);
