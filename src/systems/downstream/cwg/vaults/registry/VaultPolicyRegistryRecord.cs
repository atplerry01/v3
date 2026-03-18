namespace Whycespace.Systems.Downstream.Cwg.Vaults.Registry;

public sealed record VaultPolicyRegistryRecord(
    Guid PolicyBindingId,
    Guid VaultId,
    string PolicyId,
    string PolicyName,
    string PolicyVersion,
    string PolicyScope,
    string PolicyStatus,
    DateTime BoundAt,
    DateTime UpdatedAt,
    string? Description = null,
    string? GovernanceScope = null,
    string? ComplianceTag = null
);
