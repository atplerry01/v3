namespace Whycespace.Reliability.DeadLetter.Models;

public sealed record ReplayResult(bool Success, string Message);
