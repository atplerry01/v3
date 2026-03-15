using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

namespace Whycespace.GovernanceGuardian.Tests;

public class GuardianRecordStoreTests
{
    private readonly GuardianRecordStore _store = new();

    private GuardianRecord CreateRecord(
        Guid? id = null,
        string? identityId = null,
        GuardianRole role = GuardianRole.Guardian) =>
        new(
            id ?? Guid.NewGuid(),
            identityId ?? Guid.NewGuid().ToString(),
            "Test Guardian",
            role,
            GuardianStatus.Active,
            new List<string> { "mobility" },
            DateTime.UtcNow,
            "system",
            DateTime.UtcNow,
            null,
            new Dictionary<string, string>());

    [Fact]
    public void Save_And_GetById_Succeeds()
    {
        var record = CreateRecord();
        _store.Save(record);

        var result = _store.GetById(record.GuardianId);
        Assert.NotNull(result);
        Assert.Equal(record.GuardianId, result.GuardianId);
    }

    [Fact]
    public void Save_DuplicateId_Throws()
    {
        var id = Guid.NewGuid();
        _store.Save(CreateRecord(id: id, identityId: "id-1"));

        Assert.Throws<InvalidOperationException>(() =>
            _store.Save(CreateRecord(id: id, identityId: "id-2")));
    }

    [Fact]
    public void Save_DuplicateIdentity_Throws()
    {
        var identityId = "dup";
        _store.Save(CreateRecord(identityId: identityId));

        Assert.Throws<InvalidOperationException>(() =>
            _store.Save(CreateRecord(identityId: identityId)));
    }

    [Fact]
    public void Save_DuplicateIdentity_RollsBackGuardianId()
    {
        var identityId = "dup-rollback";
        _store.Save(CreateRecord(identityId: identityId));

        var secondId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            _store.Save(CreateRecord(id: secondId, identityId: identityId)));

        Assert.False(_store.ExistsById(secondId));
    }

    [Fact]
    public void GetByIdentity_Succeeds()
    {
        var identityId = "identity-lookup";
        var record = CreateRecord(identityId: identityId);
        _store.Save(record);

        var result = _store.GetByIdentity(identityId);
        Assert.NotNull(result);
        Assert.Equal(record.GuardianId, result.GuardianId);
    }

    [Fact]
    public void GetAll_ReturnsAllRecords()
    {
        _store.Save(CreateRecord());
        _store.Save(CreateRecord());

        var all = _store.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetByRole_FiltersCorrectly()
    {
        _store.Save(CreateRecord(role: GuardianRole.Guardian));
        _store.Save(CreateRecord(role: GuardianRole.EmergencyGuardian));
        _store.Save(CreateRecord(role: GuardianRole.Guardian));

        var result = _store.GetByRole(GuardianRole.Guardian);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetByDomain_FiltersCorrectly()
    {
        _store.Save(CreateRecord());
        _store.Save(CreateRecord());

        var result = _store.GetByDomain("mobility");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Update_Succeeds()
    {
        var record = CreateRecord();
        _store.Save(record);

        var updated = record with { GuardianStatus = GuardianStatus.Suspended };
        _store.Update(updated);

        var result = _store.GetById(record.GuardianId);
        Assert.NotNull(result);
        Assert.Equal(GuardianStatus.Suspended, result.GuardianStatus);
    }

    [Fact]
    public void Update_NotFound_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _store.Update(CreateRecord()));
    }

    [Fact]
    public void ExistsById_ReturnsTrueForExisting()
    {
        var record = CreateRecord();
        _store.Save(record);

        Assert.True(_store.ExistsById(record.GuardianId));
        Assert.False(_store.ExistsById(Guid.NewGuid()));
    }

    [Fact]
    public void ExistsByIdentity_ReturnsTrueForExisting()
    {
        var identityId = "exists-check";
        _store.Save(CreateRecord(identityId: identityId));

        Assert.True(_store.ExistsByIdentity(identityId));
        Assert.False(_store.ExistsByIdentity("nonexistent"));
    }
}
