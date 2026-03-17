namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record RevokeFederationCommand(
    Guid IdentityId,
    string ProviderName,
    string Reason,
    DateTime Timestamp);
