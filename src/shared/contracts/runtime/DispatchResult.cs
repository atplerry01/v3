namespace Whycespace.Contracts.Runtime;

public sealed record DispatchResult(
    bool Success,
    IReadOnlyDictionary<string, object> Data,
    string? Error = null)
{
    public static DispatchResult Ok(Dictionary<string, object> data)
        => new(true, data);

    public static DispatchResult Ok()
        => new(true, new Dictionary<string, object>());

    public static DispatchResult Fail(string error)
        => new(false, new Dictionary<string, object>(), error);

    public T Get<T>(string key) => (T)Data[key];

    public bool TryGet<T>(string key, out T? value)
    {
        if (Data.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }
}
