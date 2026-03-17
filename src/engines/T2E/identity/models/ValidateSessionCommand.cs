namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record ValidateSessionCommand(
    Guid SessionId,
    DateTime Timestamp);
