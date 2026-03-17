namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record ValidateSessionCommand(
    Guid SessionId,
    DateTime Timestamp);
