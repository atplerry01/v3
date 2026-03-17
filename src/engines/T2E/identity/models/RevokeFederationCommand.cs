namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record RevokeFederationCommand(
    Guid IdentityId,
    string ProviderName,
    string Reason,
    DateTime Timestamp);
