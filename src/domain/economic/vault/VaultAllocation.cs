namespace Whycespace.Domain.Economic.Vault;

public sealed class VaultAllocation
{
    public VaultAllocationId AllocationId { get; }
    public Guid VaultId { get; }
    public Guid RecipientIdentityId { get; }
    public VaultAllocationType AllocationType { get; }
    public VaultAllocationStatus Status { get; private set; }
    public decimal AllocationPercentage { get; private set; }
    public decimal AllocationAmount { get; private set; }
    public string? Description { get; }
    public string? AllocationReference { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private VaultAllocation(
        VaultAllocationId allocationId,
        Guid vaultId,
        Guid recipientIdentityId,
        VaultAllocationType allocationType,
        decimal allocationPercentage,
        decimal allocationAmount,
        string? description,
        string? allocationReference)
    {
        AllocationId = allocationId;
        VaultId = vaultId;
        RecipientIdentityId = recipientIdentityId;
        AllocationType = allocationType;
        Status = VaultAllocationStatus.Active;
        AllocationPercentage = allocationPercentage;
        AllocationAmount = allocationAmount;
        Description = description;
        AllocationReference = allocationReference;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static VaultAllocation Create(
        Guid vaultId,
        Guid recipientIdentityId,
        VaultAllocationType allocationType,
        decimal allocationPercentage,
        decimal allocationAmount,
        string? description = null,
        string? allocationReference = null)
    {
        if (vaultId == Guid.Empty)
            throw new InvalidOperationException("VaultId must be specified.");

        if (recipientIdentityId == Guid.Empty)
            throw new InvalidOperationException("RecipientIdentityId must be specified.");

        if (allocationPercentage < 0 || allocationPercentage > 100)
            throw new InvalidOperationException("AllocationPercentage must be between 0 and 100.");

        if (allocationAmount < 0)
            throw new InvalidOperationException("AllocationAmount must be >= 0.");

        return new VaultAllocation(
            VaultAllocationId.New(),
            vaultId,
            recipientIdentityId,
            allocationType,
            allocationPercentage,
            allocationAmount,
            description,
            allocationReference);
    }

    public bool IsActiveAllocation() => Status == VaultAllocationStatus.Active;

    public bool IsOwnershipAllocation() => AllocationType == VaultAllocationType.OwnershipShare;

    public void SuspendAllocation()
    {
        if (Status == VaultAllocationStatus.Closed)
            throw new InvalidOperationException("Closed allocation cannot be suspended.");

        Status = VaultAllocationStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CloseAllocation()
    {
        if (Status == VaultAllocationStatus.Closed)
            throw new InvalidOperationException("Allocation is already closed.");

        Status = VaultAllocationStatus.Closed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAllocationPercentage(decimal percentage)
    {
        if (Status == VaultAllocationStatus.Closed)
            throw new InvalidOperationException("Closed allocation cannot be updated.");

        if (percentage < 0 || percentage > 100)
            throw new InvalidOperationException("AllocationPercentage must be between 0 and 100.");

        AllocationPercentage = percentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAllocationAmount(decimal amount)
    {
        if (Status == VaultAllocationStatus.Closed)
            throw new InvalidOperationException("Closed allocation cannot be updated.");

        if (amount < 0)
            throw new InvalidOperationException("AllocationAmount must be >= 0.");

        AllocationAmount = amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
