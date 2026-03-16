namespace Whycespace.Systems.WhyceID.Models;

public sealed record AuthenticationResult(
    bool Success,
    string Message);
