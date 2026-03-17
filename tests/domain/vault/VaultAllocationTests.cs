namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Economic;
using Xunit;

public sealed class VaultAllocationTests
{
    private static VaultAllocation CreateValidAllocation(
        VaultAllocationType type = VaultAllocationType.OwnershipShare,
        decimal percentage = 25m,
        decimal amount = 0m)
    {
        return VaultAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            percentage,
            amount);
    }

    // --- AllocationCreationTest ---

    [Fact]
    public void Create_ValidParameters_CreatesAllocation()
    {
        var vaultId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        var allocation = VaultAllocation.Create(
            vaultId,
            recipientId,
            VaultAllocationType.InvestmentAllocation,
            50m,
            10000m,
            "Test allocation",
            "SPV-001");

        Assert.Equal(vaultId, allocation.VaultId);
        Assert.Equal(recipientId, allocation.RecipientIdentityId);
        Assert.Equal(VaultAllocationType.InvestmentAllocation, allocation.AllocationType);
        Assert.Equal(VaultAllocationStatus.Active, allocation.Status);
        Assert.Equal(50m, allocation.AllocationPercentage);
        Assert.Equal(10000m, allocation.AllocationAmount);
        Assert.Equal("Test allocation", allocation.Description);
        Assert.Equal("SPV-001", allocation.AllocationReference);
    }

    [Fact]
    public void Create_EmptyVaultId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAllocation.Create(Guid.Empty, Guid.NewGuid(), VaultAllocationType.OwnershipShare, 50m, 0m));
    }

    [Fact]
    public void Create_EmptyRecipientId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAllocation.Create(Guid.NewGuid(), Guid.Empty, VaultAllocationType.OwnershipShare, 50m, 0m));
    }

    // --- AllocationPercentageValidationTest ---

    [Fact]
    public void Create_NegativePercentage_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), VaultAllocationType.OwnershipShare, -1m, 0m));
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), VaultAllocationType.OwnershipShare, 101m, 0m));
    }

    [Fact]
    public void Create_BoundaryPercentage0_Succeeds()
    {
        var allocation = VaultAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), VaultAllocationType.ReserveAllocation, 0m, 0m);
        Assert.Equal(0m, allocation.AllocationPercentage);
    }

    [Fact]
    public void Create_BoundaryPercentage100_Succeeds()
    {
        var allocation = VaultAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), VaultAllocationType.OwnershipShare, 100m, 0m);
        Assert.Equal(100m, allocation.AllocationPercentage);
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), VaultAllocationType.OwnershipShare, 50m, -1m));
    }

    [Fact]
    public void UpdateAllocationPercentage_ValidValue_Updates()
    {
        var allocation = CreateValidAllocation(percentage: 25m);

        allocation.UpdateAllocationPercentage(75m);

        Assert.Equal(75m, allocation.AllocationPercentage);
    }

    [Fact]
    public void UpdateAllocationPercentage_OutOfRange_Throws()
    {
        var allocation = CreateValidAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.UpdateAllocationPercentage(101m));
        Assert.Throws<InvalidOperationException>(() => allocation.UpdateAllocationPercentage(-1m));
    }

    [Fact]
    public void UpdateAllocationPercentage_ClosedAllocation_Throws()
    {
        var allocation = CreateValidAllocation();
        allocation.CloseAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.UpdateAllocationPercentage(50m));
    }

    [Fact]
    public void UpdateAllocationAmount_ValidValue_Updates()
    {
        var allocation = CreateValidAllocation(amount: 1000m);

        allocation.UpdateAllocationAmount(5000m);

        Assert.Equal(5000m, allocation.AllocationAmount);
    }

    [Fact]
    public void UpdateAllocationAmount_Negative_Throws()
    {
        var allocation = CreateValidAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.UpdateAllocationAmount(-1m));
    }

    [Fact]
    public void UpdateAllocationAmount_ClosedAllocation_Throws()
    {
        var allocation = CreateValidAllocation();
        allocation.CloseAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.UpdateAllocationAmount(100m));
    }

    // --- AllocationSuspensionTest ---

    [Fact]
    public void SuspendAllocation_Active_TransitionsToSuspended()
    {
        var allocation = CreateValidAllocation();

        allocation.SuspendAllocation();

        Assert.Equal(VaultAllocationStatus.Suspended, allocation.Status);
        Assert.False(allocation.IsActiveAllocation());
    }

    [Fact]
    public void SuspendAllocation_Closed_Throws()
    {
        var allocation = CreateValidAllocation();
        allocation.CloseAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.SuspendAllocation());
    }

    // --- AllocationClosureTest ---

    [Fact]
    public void CloseAllocation_Active_TransitionsToClosed()
    {
        var allocation = CreateValidAllocation();

        allocation.CloseAllocation();

        Assert.Equal(VaultAllocationStatus.Closed, allocation.Status);
        Assert.False(allocation.IsActiveAllocation());
    }

    [Fact]
    public void CloseAllocation_Suspended_TransitionsToClosed()
    {
        var allocation = CreateValidAllocation();
        allocation.SuspendAllocation();

        allocation.CloseAllocation();

        Assert.Equal(VaultAllocationStatus.Closed, allocation.Status);
    }

    [Fact]
    public void CloseAllocation_AlreadyClosed_Throws()
    {
        var allocation = CreateValidAllocation();
        allocation.CloseAllocation();

        Assert.Throws<InvalidOperationException>(() => allocation.CloseAllocation());
    }

    // --- Helper Method Tests ---

    [Fact]
    public void IsActiveAllocation_ActiveStatus_ReturnsTrue()
    {
        var allocation = CreateValidAllocation();

        Assert.True(allocation.IsActiveAllocation());
    }

    [Fact]
    public void IsOwnershipAllocation_OwnershipType_ReturnsTrue()
    {
        var allocation = CreateValidAllocation(type: VaultAllocationType.OwnershipShare);

        Assert.True(allocation.IsOwnershipAllocation());
    }

    [Fact]
    public void IsOwnershipAllocation_OtherType_ReturnsFalse()
    {
        var allocation = CreateValidAllocation(type: VaultAllocationType.TreasuryAllocation);

        Assert.False(allocation.IsOwnershipAllocation());
    }

    // --- Value Object Tests ---

    [Fact]
    public void VaultAllocationId_IsStronglyTyped()
    {
        var id = VaultAllocationId.New();
        Guid guid = id;

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(id.Value, guid);
    }
}
