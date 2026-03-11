using Whycespace.Engine.Identity;
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

        var cmd = new CreateIdentityCommand(Guid.NewGuid(), IdentityType.Individual);

        var result = engine.Execute(cmd, registry);

        Assert.NotNull(result);
        Assert.Equal(cmd.IdentityId, result.IdentityId);
        Assert.Equal(IdentityType.Individual, result.Type);
    }

    [Fact]
    public void IdentityCreation_ShouldEmitEvent_WithCorrectType()
    {
        var registry = new IdentityRegistry();
        var engine = new IdentityCreationEngine();

        var cmd = new CreateIdentityCommand(Guid.NewGuid(), IdentityType.Organization);

        var result = engine.Execute(cmd, registry);

        Assert.Equal(IdentityType.Organization, result.Type);
    }

    [Fact]
    public void IdentityCreation_DuplicateId_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var engine = new IdentityCreationEngine();
        var id = Guid.NewGuid();

        engine.Execute(new CreateIdentityCommand(id, IdentityType.Individual), registry);

        Assert.Throws<InvalidOperationException>(() =>
            engine.Execute(new CreateIdentityCommand(id, IdentityType.Individual), registry));
    }
}
