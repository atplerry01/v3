namespace Whycespace.VaultRegistry.Tests;

using Whycespace.Systems.Downstream.Economic.Vault.Registry;

public sealed class VaultRegistryTests
{
    private readonly VaultRegistry _registry = new();

    private static VaultRegistryRecord CreateRecord(
        Guid? vaultId = null,
        Guid? ownerId = null,
        string purpose = "Operations",
        string status = "Active")
    {
        return new VaultRegistryRecord(
            VaultId: vaultId ?? Guid.NewGuid(),
            VaultName: "Test Vault",
            OwnerIdentityId: ownerId ?? Guid.NewGuid(),
            VaultPurpose: purpose,
            VaultStatus: status,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void RegisterVault_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterVault(record);

        var result = _registry.GetVault(record.VaultId);
        Assert.NotNull(result);
        Assert.Equal(record.VaultId, result.VaultId);
        Assert.Equal(record.VaultName, result.VaultName);
    }

    [Fact]
    public void RegisterVault_DuplicateVault_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterVault(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterVault(record));
    }

    [Fact]
    public void GetVault_ReturnsCorrectRecord()
    {
        var record = CreateRecord();
        _registry.RegisterVault(record);

        var result = _registry.GetVault(record.VaultId);

        Assert.NotNull(result);
        Assert.Equal(record.VaultName, result.VaultName);
        Assert.Equal(record.OwnerIdentityId, result.OwnerIdentityId);
        Assert.Equal(record.VaultPurpose, result.VaultPurpose);
    }

    [Fact]
    public void GetVault_NonExistent_ReturnsNull()
    {
        var result = _registry.GetVault(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetVaultsByOwner_ReturnsMatchingVaults()
    {
        var ownerId = Guid.NewGuid();
        var record1 = CreateRecord(ownerId: ownerId);
        var record2 = CreateRecord(ownerId: ownerId);
        var record3 = CreateRecord(); // different owner

        _registry.RegisterVault(record1);
        _registry.RegisterVault(record2);
        _registry.RegisterVault(record3);

        var results = _registry.GetVaultsByOwner(ownerId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(ownerId, r.OwnerIdentityId));
    }

    [Fact]
    public void GetVaultsByOwner_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetVaultsByOwner(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetVaultsByPurpose_ReturnsMatchingVaults()
    {
        var record1 = CreateRecord(purpose: "Treasury");
        var record2 = CreateRecord(purpose: "Treasury");
        var record3 = CreateRecord(purpose: "Operations");

        _registry.RegisterVault(record1);
        _registry.RegisterVault(record2);
        _registry.RegisterVault(record3);

        var results = _registry.GetVaultsByPurpose("Treasury");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("Treasury", r.VaultPurpose));
    }

    [Fact]
    public void GetVaultsByPurpose_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetVaultsByPurpose("NonExistent");
        Assert.Empty(results);
    }

    [Fact]
    public void GetVaultsByStatus_ReturnsMatchingVaults()
    {
        var record1 = CreateRecord(status: "Active");
        var record2 = CreateRecord(status: "Active");
        var record3 = CreateRecord(status: "Suspended");

        _registry.RegisterVault(record1);
        _registry.RegisterVault(record2);
        _registry.RegisterVault(record3);

        var results = _registry.GetVaultsByStatus("Active");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("Active", r.VaultStatus));
    }

    [Fact]
    public void GetVaultsByStatus_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetVaultsByStatus("Closed");
        Assert.Empty(results);
    }

    [Fact]
    public void ListVaults_ReturnsAllRegistered()
    {
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        _registry.RegisterVault(record1);
        _registry.RegisterVault(record2);

        var results = _registry.ListVaults();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListVaults_EmptyRegistry_ReturnsEmpty()
    {
        var results = _registry.ListVaults();
        Assert.Empty(results);
    }
}
