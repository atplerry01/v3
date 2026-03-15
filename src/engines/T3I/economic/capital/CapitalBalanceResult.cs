namespace Whycespace.Engines.T3I.Economic.Capital;

public sealed record CapitalBalanceResult(
    bool Success,
    CapitalBalanceSnapshot? Snapshot,
    string? Error)
{
    public static CapitalBalanceResult Ok(CapitalBalanceSnapshot snapshot)
    {
        return new CapitalBalanceResult(
            Success: true,
            Snapshot: snapshot,
            Error: null);
    }

    public static CapitalBalanceResult Fail(string error)
    {
        return new CapitalBalanceResult(
            Success: false,
            Snapshot: null,
            Error: error);
    }
}
