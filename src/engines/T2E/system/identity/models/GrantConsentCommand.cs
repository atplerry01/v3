namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record GrantConsentCommand(
    Guid IdentityId,
    string ConsentType,
    string ConsentScope,
    string GrantedBy,
    DateTime Timestamp);
