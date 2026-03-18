namespace Whycespace.Systems.WhyceID.Models;

public sealed record AuthorizationResult(
    bool Allowed,
    string Reason);
