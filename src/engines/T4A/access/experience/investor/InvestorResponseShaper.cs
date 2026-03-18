namespace Whycespace.Engines.T4A.Access.Experience.Investor;

public sealed class InvestorResponseShaper : IResponseShaper<IReadOnlyDictionary<string, object>>
{
    public string ClientType => "investor";

    private static readonly HashSet<string> InvestorVisibleKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "vaultId", "name", "spvId", "currency", "status",
        "amount", "balance", "profit", "revenue",
        "allocationId", "contributionId", "allocatedAt"
    };

    public object Shape(IReadOnlyDictionary<string, object> data)
    {
        // Investor sees financial data only — no internal IDs or system metadata
        var shaped = new Dictionary<string, object>();
        foreach (var kvp in data)
        {
            if (InvestorVisibleKeys.Contains(kvp.Key))
                shaped[kvp.Key] = kvp.Value;
        }

        shaped["_view"] = "investor";
        return shaped;
    }
}
