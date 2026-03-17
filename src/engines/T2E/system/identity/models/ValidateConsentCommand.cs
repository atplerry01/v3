namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record ValidateConsentCommand(
    Guid IdentityId,
    string ConsentType,
    string RequiredScope,
    DateTime Timestamp);
