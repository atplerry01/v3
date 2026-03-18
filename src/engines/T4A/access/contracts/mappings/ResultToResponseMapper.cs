namespace Whycespace.Engines.T4A.Access.Contracts.Mappings;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public static class ResultToResponseMapper
{
    public static CapitalAllocationResponse ToCapitalAllocation(DispatchResult result)
        => new(
            AllocationId: GetString(result, "allocationId"),
            VaultId: GetString(result, "vaultId"),
            SpvId: GetString(result, "spvId"),
            Amount: GetDecimal(result, "amount"),
            Currency: GetString(result, "currency"),
            Status: GetString(result, "status", "accepted"));

    public static VaultResponse ToVault(DispatchResult result)
        => new(
            VaultId: GetString(result, "vaultId"),
            Name: GetString(result, "name"),
            SpvId: GetString(result, "spvId"),
            Currency: GetString(result, "currency"),
            Status: GetString(result, "status", "created"));

    public static PropertyResponse ToProperty(DispatchResult result)
        => new(
            PropertyId: GetString(result, "propertyId"),
            Address: GetString(result, "address"),
            AskingPrice: GetDecimal(result, "askingPrice"),
            Currency: GetString(result, "currency"),
            Status: GetString(result, "status", "listed"));

    public static RideResponse ToRide(DispatchResult result)
        => new(
            RideId: GetString(result, "rideId"),
            PassengerId: GetString(result, "passengerId"),
            Status: GetString(result, "status", "requested"),
            DriverId: GetStringOrNull(result, "driverId"),
            EstimatedArrival: GetStringOrNull(result, "estimatedArrival"));

    public static IdentityResponse ToIdentity(DispatchResult result)
        => new(
            IdentityId: GetString(result, "identityId"),
            DisplayName: GetString(result, "displayName"),
            Email: GetString(result, "email"),
            IdentityType: GetString(result, "identityType"),
            Status: GetString(result, "status", "registered"));

    public static PolicyEvaluationResponse ToPolicyEvaluation(DispatchResult result)
    {
        var obligations = result.Data
            .Where(kvp => kvp.Key.StartsWith("obligation."))
            .ToDictionary(kvp => kvp.Key["obligation.".Length..], kvp => kvp.Value);

        return new(
            PolicyId: GetString(result, "policyId"),
            Decision: GetString(result, "decision", "unknown"),
            Reason: GetStringOrNull(result, "reason"),
            Obligations: obligations.Count > 0 ? obligations : null);
    }

    private static string GetString(DispatchResult result, string key, string fallback = "")
        => result.TryGet<string>(key, out var value) ? value ?? fallback : fallback;

    private static string? GetStringOrNull(DispatchResult result, string key)
        => result.TryGet<string>(key, out var value) ? value : null;

    private static decimal GetDecimal(DispatchResult result, string key)
        => result.Data.TryGetValue(key, out var obj) ? Convert.ToDecimal(obj) : 0m;
}
