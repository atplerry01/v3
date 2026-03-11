namespace Whycespace.Engine.Identity;

using Whycespace.System.WhyceID.Commands;
using Whycespace.System.WhyceID.Events;
using Whycespace.System.WhyceID.Registry;

public sealed class IdentityVerificationEngine
{
    public IdentityVerifiedEvent Execute(
        VerifyIdentityCommand command,
        IdentityRegistry registry)
    {
        var identity = registry.Get(command.IdentityId);

        identity.Activate();

        return new IdentityVerifiedEvent(
            command.IdentityId,
            DateTime.UtcNow);
    }
}
