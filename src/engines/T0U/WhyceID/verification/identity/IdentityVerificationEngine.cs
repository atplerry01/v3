namespace Whycespace.Engines.T0U.WhyceID.Verification.Identity;

using Whycespace.Systems.WhyceID.Registry;

public sealed class IdentityVerificationEngine
{
    private readonly IdentityRegistry _registry;

    public IdentityVerificationEngine(
        IdentityRegistry registry)
    {
        _registry = registry;
    }

    public void VerifyIdentity(Guid identityId)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}");
        }

        var identity = _registry.Get(identityId);

        identity.Verify();

        _registry.Update(identity);
    }
}
