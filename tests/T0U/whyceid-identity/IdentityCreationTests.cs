using Whycespace.Engines.T0U.WhyceID.Identity.Creation;
using Whycespace.Engines.T0U.WhyceID.Identity.Attributes;
using Whycespace.Engines.T0U.WhyceID.Identity.Graph;
using Whycespace.Engines.T0U.WhyceID.Authentication;
using Whycespace.Engines.T0U.WhyceID.Authorization.Decision;
using Whycespace.Engines.T0U.WhyceID.Consent;
using Whycespace.Engines.T0U.WhyceID.Trust.Device;
using Whycespace.Engines.T0U.WhyceID.Trust.Scoring;
using Whycespace.Engines.T0U.WhyceID.Federation.Provider;
using Whycespace.Engines.T0U.WhyceID.AccessScope.Assignment;
using Whycespace.Engines.T0U.WhyceID.Audit.Reporting;
using Whycespace.Engines.T0U.WhyceID.Recovery.Execution;
using Whycespace.Engines.T0U.WhyceID.Revocation.Execution;
using Whycespace.Engines.T0U.WhyceID.Roles.Assignment;
using Whycespace.Engines.T0U.WhyceID.Permissions.Grant;
using Whycespace.Engines.T0U.WhyceID.Policy.Enforcement;
using Whycespace.Engines.T0U.WhyceID.Verification.Identity;
using Whycespace.Engines.T0U.WhyceID.Service.Registration;
using Whycespace.Engines.T0U.WhyceID.Session.Creation;
using Whycespace.Systems.WhyceID.Commands;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityCreationTests
{
    [Fact]
    public void IdentityCreation_ShouldRegisterIdentity()
    {
        var registry = new IdentityRegistry();
        var engine = new IdentityCreationEngine();

        var cmd = new CreateIdentityCommand(Guid.NewGuid(), IdentityType.User);

        var result = engine.Execute(cmd, registry);

        Assert.NotNull(result);
        Assert.Equal(cmd.IdentityId, result.IdentityId);
        Assert.Equal(IdentityType.User, result.Type);
    }

    [Fact]
    public void IdentityCreation_ShouldEmitEvent_WithCorrectType()
    {
        var registry = new IdentityRegistry();
        var engine = new IdentityCreationEngine();

        var cmd = new CreateIdentityCommand(Guid.NewGuid(), IdentityType.Service);

        var result = engine.Execute(cmd, registry);

        Assert.Equal(IdentityType.Service, result.Type);
    }

    [Fact]
    public void IdentityCreation_DuplicateId_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var engine = new IdentityCreationEngine();
        var id = Guid.NewGuid();

        engine.Execute(new CreateIdentityCommand(id, IdentityType.User), registry);

        Assert.Throws<InvalidOperationException>(() =>
            engine.Execute(new CreateIdentityCommand(id, IdentityType.User), registry));
    }
}
