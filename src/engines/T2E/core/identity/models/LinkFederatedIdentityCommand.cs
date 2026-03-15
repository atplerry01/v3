namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record LinkFederatedIdentityCommand(
    Guid IdentityId,
    string ProviderName,
    string ExternalIdentityId,
    string ExternalEmail,
    DateTime Timestamp);
