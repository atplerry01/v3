namespace Whycespace.Engines.T2E.Economic.Capital.Shared.Models;

public sealed record CapitalPolicyRequest(
    string PolicyDomain,
    string Operation,
    CapitalPolicyContext CapitalContext,
    IReadOnlyDictionary<string, string> Metadata
);

public static class CapitalPolicyOperations
{
    public const string CommitCapital = "CommitCapital";
    public const string ContributeCapital = "ContributeCapital";
    public const string ReserveCapital = "ReserveCapital";
    public const string AllocateCapital = "AllocateCapital";
    public const string UtilizeCapital = "UtilizeCapital";
    public const string DistributeCapital = "DistributeCapital";
}
