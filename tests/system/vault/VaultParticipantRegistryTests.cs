namespace Whycespace.VaultParticipantRegistry.Tests;

using Whycespace.Systems.Downstream.Economic.Vault.Participants;

public sealed class VaultParticipantRegistryTests
{
    private readonly VaultParticipantRegistry _registry = new();

    private static VaultParticipantRegistryRecord CreateRecord(
        Guid? participantId = null,
        Guid? vaultId = null,
        Guid? identityId = null,
        string role = "Contributor",
        string status = "Active",
        decimal ownershipPercentage = 0m)
    {
        return new VaultParticipantRegistryRecord(
            ParticipantId: participantId ?? Guid.NewGuid(),
            VaultId: vaultId ?? Guid.NewGuid(),
            IdentityId: identityId ?? Guid.NewGuid(),
            ParticipantRole: role,
            ParticipantStatus: status,
            OwnershipPercentage: ownershipPercentage,
            AddedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void RegisterParticipant_SuccessfullyRegisters()
    {
        var record = CreateRecord();

        _registry.RegisterParticipant(record);

        var result = _registry.GetParticipant(record.ParticipantId);
        Assert.NotNull(result);
        Assert.Equal(record.ParticipantId, result.ParticipantId);
        Assert.Equal(record.VaultId, result.VaultId);
        Assert.Equal(record.IdentityId, result.IdentityId);
    }

    [Fact]
    public void RegisterParticipant_DuplicateParticipant_Throws()
    {
        var record = CreateRecord();
        _registry.RegisterParticipant(record);

        Assert.Throws<InvalidOperationException>(() => _registry.RegisterParticipant(record));
    }

    [Fact]
    public void GetParticipant_ReturnsCorrectRecord()
    {
        var record = CreateRecord(role: "Owner", ownershipPercentage: 50m);
        _registry.RegisterParticipant(record);

        var result = _registry.GetParticipant(record.ParticipantId);

        Assert.NotNull(result);
        Assert.Equal(record.ParticipantRole, result.ParticipantRole);
        Assert.Equal(record.OwnershipPercentage, result.OwnershipPercentage);
    }

    [Fact]
    public void GetParticipant_NonExistent_ReturnsNull()
    {
        var result = _registry.GetParticipant(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetParticipantsByVault_ReturnsMatchingParticipants()
    {
        var vaultId = Guid.NewGuid();
        var record1 = CreateRecord(vaultId: vaultId, role: "Owner");
        var record2 = CreateRecord(vaultId: vaultId, role: "Operator");
        var record3 = CreateRecord(); // different vault

        _registry.RegisterParticipant(record1);
        _registry.RegisterParticipant(record2);
        _registry.RegisterParticipant(record3);

        var results = _registry.GetParticipantsByVault(vaultId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(vaultId, r.VaultId));
    }

    [Fact]
    public void GetParticipantsByVault_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetParticipantsByVault(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetParticipantsByIdentity_ReturnsMatchingParticipants()
    {
        var identityId = Guid.NewGuid();
        var record1 = CreateRecord(identityId: identityId);
        var record2 = CreateRecord(identityId: identityId);
        var record3 = CreateRecord(); // different identity

        _registry.RegisterParticipant(record1);
        _registry.RegisterParticipant(record2);
        _registry.RegisterParticipant(record3);

        var results = _registry.GetParticipantsByIdentity(identityId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(identityId, r.IdentityId));
    }

    [Fact]
    public void GetParticipantsByIdentity_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetParticipantsByIdentity(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetParticipantsByRole_ReturnsMatchingParticipants()
    {
        var record1 = CreateRecord(role: "Auditor");
        var record2 = CreateRecord(role: "Auditor");
        var record3 = CreateRecord(role: "Investor");

        _registry.RegisterParticipant(record1);
        _registry.RegisterParticipant(record2);
        _registry.RegisterParticipant(record3);

        var results = _registry.GetParticipantsByRole("Auditor");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("Auditor", r.ParticipantRole));
    }

    [Fact]
    public void GetParticipantsByRole_NoMatch_ReturnsEmpty()
    {
        var results = _registry.GetParticipantsByRole("NonExistent");
        Assert.Empty(results);
    }

    [Fact]
    public void ListParticipants_ReturnsAllRegistered()
    {
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        _registry.RegisterParticipant(record1);
        _registry.RegisterParticipant(record2);

        var results = _registry.ListParticipants();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ListParticipants_EmptyRegistry_ReturnsEmpty()
    {
        var results = _registry.ListParticipants();
        Assert.Empty(results);
    }
}
