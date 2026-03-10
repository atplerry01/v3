namespace Whycespace.Contracts.Runtime;

public sealed record ExecutionResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyDictionary<string, object> Output
)
{
    public static ExecutionResult Ok(IReadOnlyDictionary<string, object>? output = null)
        => new(true, null, output ?? new Dictionary<string, object>());

    public static ExecutionResult Fail(string reason)
        => new(false, reason, new Dictionary<string, object>());
}
