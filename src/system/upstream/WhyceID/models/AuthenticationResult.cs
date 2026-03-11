namespace Whycespace.System.WhyceID.Models;

public sealed record AuthenticationResult(
    bool Success,
    string Message);
