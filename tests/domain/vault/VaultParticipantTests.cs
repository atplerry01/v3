namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Vault;
using Xunit;

public sealed class VaultParticipantTests
{
    private static VaultParticipant CreateParticipant(
        VaultParticipantRole role = VaultParticipantRole.Contributor,
        decimal ownershipPercentage = 0m)
    {
        return new VaultParticipant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            role,
            ownershipPercentage);
    }

    // --- ParticipantCreationTest ---

    [Fact]
    public void Create_ValidParameters_CreatesParticipant()
    {
        var vaultId = Guid.NewGuid();
        var identityId = Guid.NewGuid();

        var participant = new VaultParticipant(vaultId, identityId, VaultParticipantRole.Investor, 25m);

        Assert.Equal(vaultId, participant.VaultId);
        Assert.Equal(identityId, participant.IdentityId);
        Assert.Equal(VaultParticipantRole.Investor, participant.Role);
        Assert.Equal(VaultParticipantStatus.Active, participant.Status);
        Assert.Equal(25m, participant.OwnershipPercentage);
        Assert.NotEqual(Guid.Empty, (Guid)participant.ParticipantId);
    }

    [Fact]
    public void Create_EmptyVaultId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new VaultParticipant(Guid.Empty, Guid.NewGuid(), VaultParticipantRole.Contributor));
    }

    [Fact]
    public void Create_EmptyIdentityId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new VaultParticipant(Guid.NewGuid(), Guid.Empty, VaultParticipantRole.Contributor));
    }

    // --- ParticipantRoleTest ---

    [Theory]
    [InlineData(VaultParticipantRole.Owner)]
    [InlineData(VaultParticipantRole.Operator)]
    [InlineData(VaultParticipantRole.Contributor)]
    [InlineData(VaultParticipantRole.Investor)]
    [InlineData(VaultParticipantRole.Distributor)]
    [InlineData(VaultParticipantRole.Auditor)]
    public void Create_AllRoles_AssignedCorrectly(VaultParticipantRole role)
    {
        var participant = CreateParticipant(role: role);

        Assert.Equal(role, participant.Role);
    }

    [Fact]
    public void IsOwner_OwnerRole_ReturnsTrue()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Owner);

        Assert.True(participant.IsOwner());
    }

    [Fact]
    public void IsOwner_NonOwnerRole_ReturnsFalse()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Contributor);

        Assert.False(participant.IsOwner());
    }

    [Fact]
    public void UpdateRole_ChangesRole()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Contributor);

        participant.UpdateRole(VaultParticipantRole.Operator);

        Assert.Equal(VaultParticipantRole.Operator, participant.Role);
    }

    // --- ParticipantSuspensionTest ---

    [Fact]
    public void SuspendParticipant_ActiveParticipant_ChangeStatusToSuspended()
    {
        var participant = CreateParticipant();

        participant.SuspendParticipant();

        Assert.Equal(VaultParticipantStatus.Suspended, participant.Status);
        Assert.False(participant.IsActiveParticipant());
    }

    [Fact]
    public void SuspendParticipant_AlreadySuspended_Throws()
    {
        var participant = CreateParticipant();
        participant.SuspendParticipant();

        Assert.Throws<InvalidOperationException>(() => participant.SuspendParticipant());
    }

    [Fact]
    public void SuspendParticipant_RemovedParticipant_Throws()
    {
        var participant = CreateParticipant();
        participant.RemoveParticipant(isLastOwner: false);

        Assert.Throws<InvalidOperationException>(() => participant.SuspendParticipant());
    }

    // --- ParticipantRemovalTest ---

    [Fact]
    public void RemoveParticipant_NonOwner_SetsStatusToRemoved()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Contributor);

        participant.RemoveParticipant(isLastOwner: false);

        Assert.Equal(VaultParticipantStatus.Removed, participant.Status);
    }

    [Fact]
    public void RemoveParticipant_LastOwner_Throws()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Owner);

        Assert.Throws<InvalidOperationException>(
            () => participant.RemoveParticipant(isLastOwner: true));
    }

    [Fact]
    public void RemoveParticipant_OwnerNotLast_Succeeds()
    {
        var participant = CreateParticipant(role: VaultParticipantRole.Owner);

        participant.RemoveParticipant(isLastOwner: false);

        Assert.Equal(VaultParticipantStatus.Removed, participant.Status);
    }

    [Fact]
    public void RemoveParticipant_AlreadyRemoved_Throws()
    {
        var participant = CreateParticipant();
        participant.RemoveParticipant(isLastOwner: false);

        Assert.Throws<InvalidOperationException>(
            () => participant.RemoveParticipant(isLastOwner: false));
    }

    // --- OwnershipValidationTest ---

    [Fact]
    public void Create_ValidOwnershipPercentage_Succeeds()
    {
        var participant = CreateParticipant(ownershipPercentage: 50m);

        Assert.Equal(50m, participant.OwnershipPercentage);
    }

    [Fact]
    public void Create_OwnershipPercentageAbove100_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CreateParticipant(ownershipPercentage: 101m));
    }

    [Fact]
    public void Create_NegativeOwnershipPercentage_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CreateParticipant(ownershipPercentage: -1m));
    }

    [Fact]
    public void UpdateOwnershipPercentage_ValidValue_Updates()
    {
        var participant = CreateParticipant();

        participant.UpdateOwnershipPercentage(75m);

        Assert.Equal(75m, participant.OwnershipPercentage);
    }

    [Fact]
    public void UpdateOwnershipPercentage_Above100_Throws()
    {
        var participant = CreateParticipant();

        Assert.Throws<InvalidOperationException>(
            () => participant.UpdateOwnershipPercentage(101m));
    }

    [Fact]
    public void UpdateOwnershipPercentage_Negative_Throws()
    {
        var participant = CreateParticipant();

        Assert.Throws<InvalidOperationException>(
            () => participant.UpdateOwnershipPercentage(-5m));
    }

    // --- VaultParticipantId Value Object ---

    [Fact]
    public void VaultParticipantId_IsStronglyTyped()
    {
        var id = VaultParticipantId.New();
        Guid guid = id;

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(id.Value, guid);
    }

    [Fact]
    public void IsActiveParticipant_ActiveStatus_ReturnsTrue()
    {
        var participant = CreateParticipant();

        Assert.True(participant.IsActiveParticipant());
    }

    [Fact]
    public void IsActiveParticipant_SuspendedStatus_ReturnsFalse()
    {
        var participant = CreateParticipant();
        participant.SuspendParticipant();

        Assert.False(participant.IsActiveParticipant());
    }
}
