namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record RevokeFederationCommand(
    Guid IdentityId,
    string ProviderName,
    string Reason,
    DateTime Timestamp);
