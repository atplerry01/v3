using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceRoleEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceRoleStore _roleStore = new();
    private readonly GovernanceRoleEngine _engine;
    private readonly GuardianRegistryEngine _guardianEngine;

    public GovernanceRoleEngineTests()
    {
        _engine = new GovernanceRoleEngine(_roleStore, _guardianStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        _identityRegistry.Register(identity);
        _guardianEngine.RegisterGuardian("g-1", identityId, "Alice", new List<string>());
    }

    [Fact]
    public void CreateRole_Succeeds()
    {
        var role = _engine.CreateRole("guardian", "Guardian", "Standard guardian role", new List<string> { "vote", "propose" });

        Assert.Equal("guardian", role.RoleId);
        Assert.Equal("Guardian", role.Name);
        Assert.Contains("vote", role.Permissions);
        Assert.Contains("propose", role.Permissions);
    }

    [Fact]
    public void CreateRole_Duplicate_Throws()
    {
        _engine.CreateRole("dup", "Dup", "Dup role", new List<string>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateRole("dup", "Dup2", "Dup2", new List<string>()));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void AssignRole_Succeeds()
    {
        _engine.CreateRole("senior", "SeniorGuardian", "Senior role", new List<string> { "emergency" });

        _engine.AssignRole("g-1", "senior");

        var roles = _engine.GetGuardianRoles("g-1");
        Assert.Single(roles);
        Assert.Equal("senior", roles[0].RoleId);
    }

    [Fact]
    public void AssignRole_InvalidGuardian_Throws()
    {
        _engine.CreateRole("r1", "R1", "Desc", new List<string>());

        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.AssignRole("nonexistent", "r1"));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void AssignRole_InvalidRole_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.AssignRole("g-1", "nonexistent"));
        Assert.Contains("Role not found", ex.Message);
    }

    [Fact]
    public void RevokeRole_Succeeds()
    {
        _engine.CreateRole("temp", "Temp", "Temporary", new List<string>());
        _engine.AssignRole("g-1", "temp");

        _engine.RevokeRole("g-1", "temp");

        var roles = _engine.GetGuardianRoles("g-1");
        Assert.Empty(roles);
    }

    [Fact]
    public void MultipleRoles_Supported()
    {
        _engine.CreateRole("r-a", "RoleA", "A", new List<string> { "vote" });
        _engine.CreateRole("r-b", "RoleB", "B", new List<string> { "audit" });

        _engine.AssignRole("g-1", "r-a");
        _engine.AssignRole("g-1", "r-b");

        var roles = _engine.GetGuardianRoles("g-1");
        Assert.Equal(2, roles.Count);
    }
}
