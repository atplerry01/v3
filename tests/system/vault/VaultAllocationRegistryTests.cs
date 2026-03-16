namespace Whycespace.VaultRegistry.Tests;

using Whycespace.Systems.Downstream.Economic.Vault.Allocation;

public sealed class VaultAllocationRegistryTests
{
    private readonly VaultAllocationRegistry _registry = new();

    private static VaultAllocationRegistryRecord CreateRecord(
        Guid? allocationId = null,
        Guid? vaultId = null,
        Guid? recipientId = null,
        string allocationType = "Ownership",
        string status = "Active",
        decimal percentage = 50m,
        decimal amount = 0m)
    {
        return new VaultAllocationRegistryRecord(
            AllocationId: allocationId ?? Guid.NewGuid(),
            VaultId: vaultId ?? Guid.NewGuid(),
            RecipientIdentityId: recipientId ?? Guid.NewGuid(),
            AllocationType: allocationType,
            AllocationStatus: status,
            AllocationPercentage: percentage,
            AllocationAmount: amount,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void RegisterAllocation_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterAllocation(record);

        var result = _registry.GetAllocation(record.AllocationId);
        Assert.NotNull(result);
        Assert.Equal(record.AllocationId, result.AllocationId);
        Assert.Equal(record.VaultId, result.VaultId);
    }

    [Fact]
    public void RegisterAllocation_DuplicateAllocation_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterAllocation(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterAllocation(record));
    }

    [Fact]
    public void GetAllocation_ReturnsCorrectRecord()
    {
        var record = CreateRecord();
        _registry.RegisterAllocation(record);

        var result = _registry.GetAllocation(record.AllocationId);

        Assert.NotNull(result);
        Assert.Equal(record.RecipientIdentityId, result.RecipientIdentityId);
        Assert.Equal(record.AllocationType, result.AllocationType);
        Assert.Equal(record.AllocationPercentage, result.AllocationPercentage);
    }

    [Fact]
    public void GetAllocation_NonExistent_ReturnsNull()
    {
        var result = _registry.GetAllocation(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetAllocationsByVault_ReturnsMatchingAllocations()
    {
        var vaultId = Guid.NewGuid();
        var record1 = CreateRecord(vaultId: vaultId);
        var record2 = CreateRecord(vaultId: vaultId);
        var record3 = CreateRecord(); // different vault

        _registry.RegisterAllocation(record1);
        _registry.RegisterAllocation(record2);
        _registry.RegisterAllocation(record3);

        var results = _registry.GetAllocationsByVault(vaultId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(vaultId, r.VaultId));
    }

    [Fact]
    public void GetAllocationsByVault_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetAllocationsByVault(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetAllocationsByRecipient_ReturnsMatchingAllocations()
    {
        var recipientId = Guid.NewGuid();
        var record1 = CreateRecord(recipientId: recipientId);
        var record2 = CreateRecord(recipientId: recipientId);
        var record3 = CreateRecord(); // different recipient

        _registry.RegisterAllocation(record1);
        _registry.RegisterAllocation(record2);
        _registry.RegisterAllocation(record3);

        var results = _registry.GetAllocationsByRecipient(recipientId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(recipientId, r.RecipientIdentityId));
    }

    [Fact]
    public void GetAllocationsByRecipient_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetAllocationsByRecipient(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetAllocationsByType_ReturnsMatchingAllocations()
    {
        var record1 = CreateRecord(allocationType: "Ownership");
        var record2 = CreateRecord(allocationType: "Ownership");
        var record3 = CreateRecord(allocationType: "Treasury");

        _registry.RegisterAllocation(record1);
        _registry.RegisterAllocation(record2);
        _registry.RegisterAllocation(record3);

        var results = _registry.GetAllocationsByType("Ownership");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("Ownership", r.AllocationType));
    }

    [Fact]
    public void GetAllocationsByType_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetAllocationsByType("NonExistent");
        Assert.Empty(results);
    }

    [Fact]
    public void ListAllocations_ReturnsAllRegistered()
    {
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        _registry.RegisterAllocation(record1);
        _registry.RegisterAllocation(record2);

        var results = _registry.ListAllocations();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListAllocations_EmptyRegistry_ReturnsEmpty()
    {
        var results = _registry.ListAllocations();
        Assert.Empty(results);
    }
}
