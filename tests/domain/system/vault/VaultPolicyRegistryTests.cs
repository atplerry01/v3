namespace Whycespace.VaultRegistry.Tests;

using Whycespace.Systems.Downstream.Economic.Vault.Registry;

public sealed class VaultPolicyRegistryTests
{
    private readonly VaultPolicyRegistry _registry = new();

    private static VaultPolicyRegistryRecord CreateRecord(
        Guid? policyBindingId = null,
        Guid? vaultId = null,
        string policyId = "POL-001",
        string policyName = "Test Policy",
        string policyVersion = "1.0",
        string policyScope = "Operational",
        string policyStatus = "Active")
    {
        return new VaultPolicyRegistryRecord(
            PolicyBindingId: policyBindingId ?? Guid.NewGuid(),
            VaultId: vaultId ?? Guid.NewGuid(),
            PolicyId: policyId,
            PolicyName: policyName,
            PolicyVersion: policyVersion,
            PolicyScope: policyScope,
            PolicyStatus: policyStatus,
            BoundAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void RegisterPolicyBinding_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterPolicyBinding(record);

        var result = _registry.GetPolicyBinding(record.PolicyBindingId);
        Assert.NotNull(result);
        Assert.Equal(record.PolicyBindingId, result.PolicyBindingId);
        Assert.Equal(record.VaultId, result.VaultId);
        Assert.Equal(record.PolicyId, result.PolicyId);
    }

    [Fact]
    public void RegisterPolicyBinding_DuplicateBinding_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterPolicyBinding(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterPolicyBinding(record));
    }

    [Fact]
    public void GetPolicyBinding_ReturnsCorrectRecord()
    {
        var record = CreateRecord();
        _registry.RegisterPolicyBinding(record);

        var result = _registry.GetPolicyBinding(record.PolicyBindingId);

        Assert.NotNull(result);
        Assert.Equal(record.PolicyName, result.PolicyName);
        Assert.Equal(record.PolicyVersion, result.PolicyVersion);
        Assert.Equal(record.PolicyScope, result.PolicyScope);
    }

    [Fact]
    public void GetPolicyBinding_NonExistent_ReturnsNull()
    {
        var result = _registry.GetPolicyBinding(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetPoliciesByVault_ReturnsMatchingBindings()
    {
        var vaultId = Guid.NewGuid();
        var record1 = CreateRecord(vaultId: vaultId, policyId: "POL-001");
        var record2 = CreateRecord(vaultId: vaultId, policyId: "POL-002");
        var record3 = CreateRecord(); // different vault

        _registry.RegisterPolicyBinding(record1);
        _registry.RegisterPolicyBinding(record2);
        _registry.RegisterPolicyBinding(record3);

        var results = _registry.GetPoliciesByVault(vaultId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(vaultId, r.VaultId));
    }

    [Fact]
    public void GetPoliciesByVault_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetPoliciesByVault(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetVaultsByPolicy_ReturnsMatchingBindings()
    {
        var policyId = "POL-SHARED";
        var record1 = CreateRecord(policyId: policyId);
        var record2 = CreateRecord(policyId: policyId);
        var record3 = CreateRecord(policyId: "POL-OTHER");

        _registry.RegisterPolicyBinding(record1);
        _registry.RegisterPolicyBinding(record2);
        _registry.RegisterPolicyBinding(record3);

        var results = _registry.GetVaultsByPolicy(policyId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(policyId, r.PolicyId));
    }

    [Fact]
    public void GetVaultsByPolicy_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetVaultsByPolicy("NonExistent");
        Assert.Empty(results);
    }

    [Fact]
    public void ListPolicyBindings_ReturnsAllRegistered()
    {
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        _registry.RegisterPolicyBinding(record1);
        _registry.RegisterPolicyBinding(record2);

        var results = _registry.ListPolicyBindings();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListPolicyBindings_EmptyRegistry_ReturnsEmpty()
    {
        var results = _registry.ListPolicyBindings();
        Assert.Empty(results);
    }
}
