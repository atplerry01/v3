namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record RevokeServiceIdentityCommand(
    Guid ServiceIdentityId,
    string Reason,
    DateTime Timestamp);
