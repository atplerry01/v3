namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record ValidateSessionCommand(
    Guid SessionId,
    DateTime Timestamp);
