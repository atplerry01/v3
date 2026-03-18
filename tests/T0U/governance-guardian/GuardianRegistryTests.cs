using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Registry;
using Whycespace.Systems.Upstream.Governance.Stores;

namespace Whycespace.GovernanceGuardian.Tests;

public class GuardianRegistryTests
{
    private readonly GuardianRecordStore _store = new();
    private readonly GuardianRegistry _registry;

    public GuardianRegistryTests()
    {
        _registry = new GuardianRegistry(_store);
    }

    private GuardianRecord CreateRecord(
        Guid? id = null,
        string? identityId = null,
        GuardianRole role = GuardianRole.Guardian,
        GuardianStatus status = GuardianStatus.Active,
        IReadOnlyList<string>? domains = null) =>
        new(
            id ?? Guid.NewGuid(),
            identityId ?? Guid.NewGuid().ToString(),
            "Test Guardian",
            role,
            status,
            domains ?? new List<string> { "mobility" },
            DateTime.UtcNow,
            "system",
            DateTime.UtcNow,
            null,
            new Dictionary<string, string>());

    [Fact]
    public void RegisterGuardian_Succeeds()
    {
        var record = CreateRecord();

        _registry.RegisterGuardian(record);

        var retrieved = _registry.GetGuardian(record.GuardianId);
        Assert.NotNull(retrieved);
        Assert.Equal(record.GuardianId, retrieved.GuardianId);
        Assert.Equal(record.IdentityId, retrieved.IdentityId);
    }

    [Fact]
    public void RegisterGuardian_DuplicateId_Throws()
    {
        var id = Guid.NewGuid();
        _registry.RegisterGuardian(CreateRecord(id: id, identityId: "id-1"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterGuardian(CreateRecord(id: id, identityId: "id-2")));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void RegisterGuardian_DuplicateIdentity_Throws()
    {
        var identityId = "dup-identity";
        _registry.RegisterGuardian(CreateRecord(identityId: identityId));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterGuardian(CreateRecord(identityId: identityId)));
        Assert.Contains("already has a registered guardian", ex.Message);
    }

    [Fact]
    public void RegisterGuardian_EmptyDomains_Throws()
    {
        var record = CreateRecord(domains: new List<string>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterGuardian(record));
        Assert.Contains("at least one authority domain", ex.Message);
    }

    [Fact]
    public void GetGuardian_NotFound_ReturnsNull()
    {
        var result = _registry.GetGuardian(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetGuardianByIdentity_Succeeds()
    {
        var identityId = "lookup-identity";
        var record = CreateRecord(identityId: identityId);
        _registry.RegisterGuardian(record);

        var result = _registry.GetGuardianByIdentity(identityId);
        Assert.NotNull(result);
        Assert.Equal(identityId, result.IdentityId);
    }

    [Fact]
    public void GetGuardianByIdentity_NotFound_ReturnsNull()
    {
        var result = _registry.GetGuardianByIdentity("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void GetGuardians_ReturnsAll()
    {
        _registry.RegisterGuardian(CreateRecord());
        _registry.RegisterGuardian(CreateRecord());
        _registry.RegisterGuardian(CreateRecord());

        var all = _registry.GetGuardians();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void GetGuardiansByRole_FiltersCorrectly()
    {
        _registry.RegisterGuardian(CreateRecord(role: GuardianRole.Guardian));
        _registry.RegisterGuardian(CreateRecord(role: GuardianRole.ConstitutionGuardian));
        _registry.RegisterGuardian(CreateRecord(role: GuardianRole.Guardian));

        var guardians = _registry.GetGuardiansByRole(GuardianRole.Guardian);
        Assert.Equal(2, guardians.Count);
        Assert.All(guardians, g => Assert.Equal(GuardianRole.Guardian, g.GuardianRole));
    }

    [Fact]
    public void GetGuardiansByDomain_FiltersCorrectly()
    {
        _registry.RegisterGuardian(CreateRecord(domains: new List<string> { "mobility", "energy" }));
        _registry.RegisterGuardian(CreateRecord(domains: new List<string> { "property" }));
        _registry.RegisterGuardian(CreateRecord(domains: new List<string> { "mobility" }));

        var mobilityGuardians = _registry.GetGuardiansByDomain("mobility");
        Assert.Equal(2, mobilityGuardians.Count);
    }

    [Fact]
    public void UpdateGuardianStatus_Succeeds()
    {
        var record = CreateRecord(status: GuardianStatus.Active);
        _registry.RegisterGuardian(record);

        _registry.UpdateGuardianStatus(record.GuardianId, GuardianStatus.Suspended);

        var updated = _registry.GetGuardian(record.GuardianId);
        Assert.NotNull(updated);
        Assert.Equal(GuardianStatus.Suspended, updated.GuardianStatus);
    }

    [Fact]
    public void UpdateGuardianStatus_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _registry.UpdateGuardianStatus(Guid.NewGuid(), GuardianStatus.Revoked));
        Assert.Contains("not found", ex.Message);
    }
}
