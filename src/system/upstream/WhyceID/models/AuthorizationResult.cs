namespace Whycespace.System.WhyceID.Models;

public sealed record AuthorizationResult(
    bool Allowed,
    string Reason);
