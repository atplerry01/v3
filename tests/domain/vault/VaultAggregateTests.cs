namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Vault;
using Xunit;

public sealed class VaultAggregateTests
{
    private static VaultAggregate CreateValidVault(
        string name = "Test Vault",
        VaultPurposeType purpose = VaultPurposeType.GeneralPurpose,
        string currency = "GBP",
        Guid? ownerId = null)
    {
        return VaultAggregate.Create(name, purpose, currency, ownerId ?? Guid.NewGuid());
    }

    // --- VaultCreationTest ---

    [Fact]
    public void Create_ValidParameters_CreatesVault()
    {
        var ownerId = Guid.NewGuid();

        var vault = VaultAggregate.Create("My Vault", VaultPurposeType.SPVCapital, "GBP", ownerId);

        Assert.Equal("My Vault", vault.VaultName);
        Assert.Equal(VaultPurposeType.SPVCapital, vault.Purpose);
        Assert.Equal(VaultStatus.Active, vault.Status);
        Assert.Equal(0m, vault.Balance.Amount);
        Assert.Equal("GBP", vault.Balance.Currency);
        Assert.Single(vault.Participants);
        Assert.Equal(ownerId, vault.Participants[0].IdentityId);
        Assert.Equal(VaultParticipantRole.Owner, vault.Participants[0].Role);
        Assert.Empty(vault.TransactionHistory);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAggregate.Create("", VaultPurposeType.GeneralPurpose, "GBP", Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyCurrency_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAggregate.Create("Vault", VaultPurposeType.GeneralPurpose, "", Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyOwnerId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => VaultAggregate.Create("Vault", VaultPurposeType.GeneralPurpose, "GBP", Guid.Empty));
    }

    // --- BalanceInvariantTest ---

    [Fact]
    public void UpdateBalance_NegativeAmount_Throws()
    {
        var vault = CreateValidVault();

        Assert.Throws<InvalidOperationException>(() => vault.UpdateBalance(-100m));
    }

    [Fact]
    public void UpdateBalance_ValidAmount_UpdatesBalance()
    {
        var vault = CreateValidVault();

        vault.UpdateBalance(500m);

        Assert.Equal(500m, vault.Balance.Amount);
    }

    [Fact]
    public void UpdateBalance_ClosedVault_Throws()
    {
        var vault = CreateValidVault();
        vault.CloseVault();

        Assert.Throws<InvalidOperationException>(() => vault.UpdateBalance(100m));
    }

    [Fact]
    public void UpdateBalance_FrozenVault_AllowsIncrease()
    {
        var vault = CreateValidVault();
        vault.UpdateBalance(500m);
        vault.FreezeVault();

        vault.UpdateBalance(600m);

        Assert.Equal(600m, vault.Balance.Amount);
    }

    [Fact]
    public void UpdateBalance_FrozenVault_PreventsWithdrawal()
    {
        var vault = CreateValidVault();
        vault.UpdateBalance(500m);
        vault.FreezeVault();

        Assert.Throws<InvalidOperationException>(() => vault.UpdateBalance(400m));
    }

    // --- FreezeVaultTest ---

    [Fact]
    public void FreezeVault_ActiveVault_ChangeStatusToFrozen()
    {
        var vault = CreateValidVault();

        vault.FreezeVault();

        Assert.Equal(VaultStatus.Frozen, vault.Status);
    }

    [Fact]
    public void FreezeVault_AlreadyFrozen_Throws()
    {
        var vault = CreateValidVault();
        vault.FreezeVault();

        Assert.Throws<InvalidOperationException>(() => vault.FreezeVault());
    }

    [Fact]
    public void FreezeVault_ClosedVault_Throws()
    {
        var vault = CreateValidVault();
        vault.CloseVault();

        Assert.Throws<InvalidOperationException>(() => vault.FreezeVault());
    }

    [Fact]
    public void UnfreezeVault_FrozenVault_ChangesStatusToActive()
    {
        var vault = CreateValidVault();
        vault.FreezeVault();

        vault.UnfreezeVault();

        Assert.Equal(VaultStatus.Active, vault.Status);
    }

    [Fact]
    public void UnfreezeVault_ActiveVault_Throws()
    {
        var vault = CreateValidVault();

        Assert.Throws<InvalidOperationException>(() => vault.UnfreezeVault());
    }

    // --- CloseVault ---

    [Fact]
    public void CloseVault_ZeroBalance_Closes()
    {
        var vault = CreateValidVault();

        vault.CloseVault();

        Assert.Equal(VaultStatus.Closed, vault.Status);
    }

    [Fact]
    public void CloseVault_NonZeroBalance_Throws()
    {
        var vault = CreateValidVault();
        vault.UpdateBalance(100m);

        Assert.Throws<InvalidOperationException>(() => vault.CloseVault());
    }

    [Fact]
    public void CloseVault_AlreadyClosed_Throws()
    {
        var vault = CreateValidVault();
        vault.CloseVault();

        Assert.Throws<InvalidOperationException>(() => vault.CloseVault());
    }

    // --- ParticipantRulesTest ---

    [Fact]
    public void AddParticipant_NewParticipant_AddsSuccessfully()
    {
        var vault = CreateValidVault();
        var investorId = Guid.NewGuid();

        vault.AddParticipant(investorId, VaultParticipantRole.Investor);

        Assert.Equal(2, vault.Participants.Count);
        Assert.Contains(vault.Participants, p => p.IdentityId == investorId && p.Role == VaultParticipantRole.Investor);
    }

    [Fact]
    public void AddParticipant_DuplicateParticipant_Throws()
    {
        var ownerId = Guid.NewGuid();
        var vault = VaultAggregate.Create("Vault", VaultPurposeType.GeneralPurpose, "GBP", ownerId);

        Assert.Throws<InvalidOperationException>(
            () => vault.AddParticipant(ownerId, VaultParticipantRole.Operator));
    }

    [Fact]
    public void RemoveParticipant_NonOwner_SetsStatusToRemoved()
    {
        var vault = CreateValidVault();
        var investorId = Guid.NewGuid();
        vault.AddParticipant(investorId, VaultParticipantRole.Investor);

        vault.RemoveParticipant(investorId);

        var removed = vault.Participants.First(p => p.IdentityId == investorId);
        Assert.Equal(VaultParticipantStatus.Removed, removed.Status);
    }

    [Fact]
    public void RemoveParticipant_LastOwner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var vault = VaultAggregate.Create("Vault", VaultPurposeType.GeneralPurpose, "GBP", ownerId);

        Assert.Throws<InvalidOperationException>(() => vault.RemoveParticipant(ownerId));
    }

    [Fact]
    public void RemoveParticipant_NotFound_Throws()
    {
        var vault = CreateValidVault();

        Assert.Throws<InvalidOperationException>(
            () => vault.RemoveParticipant(Guid.NewGuid()));
    }

    [Fact]
    public void SuspendParticipant_ActiveParticipant_Suspends()
    {
        var vault = CreateValidVault();
        var operatorId = Guid.NewGuid();
        vault.AddParticipant(operatorId, VaultParticipantRole.Operator);

        vault.SuspendParticipant(operatorId);

        var suspended = vault.Participants.First(p => p.IdentityId == operatorId);
        Assert.Equal(VaultParticipantStatus.Suspended, suspended.Status);
    }

    // --- TransactionReference ---

    [Fact]
    public void AddTransactionReference_ActiveVault_AddsSuccessfully()
    {
        var vault = CreateValidVault();
        var txId = Guid.NewGuid();

        vault.AddTransactionReference(txId, "Contribution");

        Assert.Single(vault.TransactionHistory);
        Assert.Equal(txId, vault.TransactionHistory[0].TransactionId);
        Assert.Equal("Contribution", vault.TransactionHistory[0].TransactionType);
    }

    [Fact]
    public void AddTransactionReference_ClosedVault_Throws()
    {
        var vault = CreateValidVault();
        vault.CloseVault();

        Assert.Throws<InvalidOperationException>(
            () => vault.AddTransactionReference(Guid.NewGuid(), "Contribution"));
    }

    // --- Value Object Invariants ---

    [Fact]
    public void VaultBalance_NegativeAmount_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new VaultBalance(-1m, "GBP"));
    }

    [Fact]
    public void VaultBalance_EmptyCurrency_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new VaultBalance(100m, ""));
    }

    [Fact]
    public void VaultId_IsStronglyTyped()
    {
        var id = VaultId.New();
        Guid guid = id;

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(id.Value, guid);
    }

    // --- Purpose Immutability ---

    [Fact]
    public void Purpose_IsSetAtCreation_AndDoesNotChange()
    {
        var vault = VaultAggregate.Create("Vault", VaultPurposeType.Escrow, "GBP", Guid.NewGuid());

        Assert.Equal(VaultPurposeType.Escrow, vault.Purpose);
    }
}
