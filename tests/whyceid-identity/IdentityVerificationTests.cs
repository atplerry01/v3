using Whycespace.Engine.Identity;
using Whycespace.System.WhyceID.Commands;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityVerificationTests
{
    [Fact]
    public void IdentityVerification_ShouldActivateIdentity()
    {
        var registry = new IdentityRegistry();
        var creationEngine = new IdentityCreationEngine();
        var verificationEngine = new IdentityVerificationEngine();

        var id = Guid.NewGuid();
        creationEngine.Execute(new CreateIdentityCommand(id, IdentityType.Individual), registry);

        var result = verificationEngine.Execute(new VerifyIdentityCommand(id), registry);

        Assert.NotNull(result);
        Assert.Equal(id, result.IdentityId);

        var identity = registry.Get(id);
        Assert.Equal(IdentityStatus.Active, identity.Status);
    }

    [Fact]
    public void IdentityVerification_AlreadyActive_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var creationEngine = new IdentityCreationEngine();
        var verificationEngine = new IdentityVerificationEngine();

        var id = Guid.NewGuid();
        creationEngine.Execute(new CreateIdentityCommand(id, IdentityType.Individual), registry);
        verificationEngine.Execute(new VerifyIdentityCommand(id), registry);

        Assert.Throws<InvalidOperationException>(() =>
            verificationEngine.Execute(new VerifyIdentityCommand(id), registry));
    }

    [Fact]
    public void IdentityVerification_NotFound_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var verificationEngine = new IdentityVerificationEngine();

        Assert.Throws<KeyNotFoundException>(() =>
            verificationEngine.Execute(new VerifyIdentityCommand(Guid.NewGuid()), registry));
    }
}
