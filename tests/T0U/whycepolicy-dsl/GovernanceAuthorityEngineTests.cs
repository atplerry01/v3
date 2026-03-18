using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Authority;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class GovernanceAuthorityEngineTests
{
    private readonly GovernanceAuthorityStore _store = new();
    private readonly GovernanceAuthorityEngine _engine;

    public GovernanceAuthorityEngineTests()
    {
        _engine = new GovernanceAuthorityEngine(_store);
    }

    [Fact]
    public void AssignAuthority_ReturnsRecord()
    {
        var record = _engine.AssignAuthority("actor-1", GovernanceRole.PolicyAuthor);

        Assert.Equal("actor-1", record.ActorId);
        Assert.Equal(GovernanceRole.PolicyAuthor, record.Role);
    }

    [Fact]
    public void GetRoles_ReturnsAssignedRoles()
    {
        _engine.AssignAuthority("actor-2", GovernanceRole.PolicyAuthor);
        _engine.AssignAuthority("actor-2", GovernanceRole.PolicyApprover);

        var roles = _engine.GetRoles("actor-2");

        Assert.Equal(2, roles.Count);
    }

    [Fact]
    public void ValidateAuthority_CorrectRole_DoesNotThrow()
    {
        _engine.AssignAuthority("actor-3", GovernanceRole.PolicyApprover);

        _engine.ValidateAuthority("actor-3", GovernanceRole.PolicyApprover);
    }

    [Fact]
    public void ValidateAuthority_MissingRole_Throws()
    {
        _engine.AssignAuthority("actor-4", GovernanceRole.PolicyAuthor);

        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            _engine.ValidateAuthority("actor-4", GovernanceRole.PolicyActivator));
        Assert.Contains("lacks required governance role", ex.Message);
    }

    [Fact]
    public void Administrator_HasAllPrivileges()
    {
        _engine.AssignAuthority("admin-1", GovernanceRole.PolicyAdministrator);

        Assert.True(_engine.HasAuthority("admin-1", GovernanceRole.PolicyAuthor));
        Assert.True(_engine.HasAuthority("admin-1", GovernanceRole.PolicyApprover));
        Assert.True(_engine.HasAuthority("admin-1", GovernanceRole.PolicyActivator));
        Assert.True(_engine.HasAuthority("admin-1", GovernanceRole.PolicyAdministrator));
    }

    [Fact]
    public void MultipleRolesPerActor_Supported()
    {
        _engine.AssignAuthority("actor-5", GovernanceRole.PolicyAuthor);
        _engine.AssignAuthority("actor-5", GovernanceRole.PolicyApprover);
        _engine.AssignAuthority("actor-5", GovernanceRole.PolicyActivator);

        Assert.True(_engine.HasAuthority("actor-5", GovernanceRole.PolicyAuthor));
        Assert.True(_engine.HasAuthority("actor-5", GovernanceRole.PolicyApprover));
        Assert.True(_engine.HasAuthority("actor-5", GovernanceRole.PolicyActivator));
        Assert.False(_engine.HasAuthority("actor-5", GovernanceRole.PolicyAdministrator));
    }

    [Fact]
    public void MultipleActors_TrackedIndependently()
    {
        _engine.AssignAuthority("actor-a", GovernanceRole.PolicyAuthor);
        _engine.AssignAuthority("actor-b", GovernanceRole.PolicyApprover);

        Assert.True(_engine.HasAuthority("actor-a", GovernanceRole.PolicyAuthor));
        Assert.False(_engine.HasAuthority("actor-a", GovernanceRole.PolicyApprover));
        Assert.True(_engine.HasAuthority("actor-b", GovernanceRole.PolicyApprover));
        Assert.False(_engine.HasAuthority("actor-b", GovernanceRole.PolicyAuthor));
    }
}
