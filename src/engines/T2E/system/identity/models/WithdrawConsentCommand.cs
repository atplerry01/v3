namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record WithdrawConsentCommand(
    Guid IdentityId,
    string ConsentType,
    string Reason,
    DateTime Timestamp);
