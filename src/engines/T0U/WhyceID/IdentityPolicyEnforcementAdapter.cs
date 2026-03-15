namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityPolicyEnforcementAdapter
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityRevocationStore _revocationStore;

    private const int MinimumTrustScore = 50;

    public IdentityPolicyEnforcementAdapter(
        IdentityRegistry registry,
        IdentityRoleStore roleStore,
        IdentityTrustStore trustStore,
        IdentityRevocationStore revocationStore)
    {
        _registry = registry;
        _roleStore = roleStore;
        _trustStore = trustStore;
        _revocationStore = revocationStore;
    }

    public IdentityPolicyContext BuildContext(Guid identityId)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        var roles = _roleStore.GetRoles(identityId);
        var trustScore = _trustStore.Get(identityId);
        var identity = _registry.Get(identityId);

        return new IdentityPolicyContext(
            identityId,
            identity.Status,
            trustScore?.Score ?? 0,
            roles,
            new Dictionary<string, string>(),
            0,
            0,
            string.Empty,
            string.Empty,
            DateTime.UtcNow
        );
    }

    public bool EvaluateIdentityAccess(Guid identityId)
    {
        var context = BuildContext(identityId);
        return Evaluate(context);
    }

    private static bool Evaluate(IdentityPolicyContext context)
    {
        if (context.IdentityStatus == IdentityStatus.Revoked)
            return false;

        if (context.IdentityStatus != IdentityStatus.Verified)
            return false;

        if (context.TrustScore < MinimumTrustScore)
            return false;

        return true;
    }
}
