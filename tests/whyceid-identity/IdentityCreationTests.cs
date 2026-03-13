using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Commands;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

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
