namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record RevokeServiceIdentityCommand(
    Guid ServiceIdentityId,
    string Reason,
    DateTime Timestamp);
