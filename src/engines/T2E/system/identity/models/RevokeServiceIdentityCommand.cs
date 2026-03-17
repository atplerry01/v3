namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record RevokeServiceIdentityCommand(
    Guid ServiceIdentityId,
    string Reason,
    DateTime Timestamp);
