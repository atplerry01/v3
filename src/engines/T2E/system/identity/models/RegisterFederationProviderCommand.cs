namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record RegisterFederationProviderCommand(
    string ProviderName,
    string ProviderType,
    string ProviderDomain,
    string CreatedBy,
    DateTime Timestamp);
