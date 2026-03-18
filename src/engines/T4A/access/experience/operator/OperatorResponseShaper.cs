namespace Whycespace.Engines.T4A.Access.Experience.Operator;

public sealed class OperatorResponseShaper : IResponseShaper<IReadOnlyDictionary<string, object>>
{
    public string ClientType => "operator";

    private static readonly HashSet<string> ExcludedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "password", "secret", "credential"
    };

    public object Shape(IReadOnlyDictionary<string, object> data)
    {
        // Operator sees operational data — excludes secrets
        var shaped = new Dictionary<string, object>();
        foreach (var kvp in data)
        {
            if (!ExcludedKeys.Contains(kvp.Key))
                shaped[kvp.Key] = kvp.Value;
        }

        shaped["_view"] = "operator";
        return shaped;
    }
}
