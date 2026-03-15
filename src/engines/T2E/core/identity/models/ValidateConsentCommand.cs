namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record ValidateConsentCommand(
    Guid IdentityId,
    string ConsentType,
    string RequiredScope,
    DateTime Timestamp);
