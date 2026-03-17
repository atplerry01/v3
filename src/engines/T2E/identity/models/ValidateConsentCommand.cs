namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record ValidateConsentCommand(
    Guid IdentityId,
    string ConsentType,
    string RequiredScope,
    DateTime Timestamp);
