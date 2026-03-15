namespace Whycespace.System.WhyceID.Adapters;

using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class IdentityPolicyEnforcementAdapter
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityRevocationStore _revocationStore;
    private readonly IdentityAttributeStore _attributeStore;
    private readonly IdentityDeviceStore _deviceStore;
    private readonly IdentitySessionStore _sessionStore;

    private const string PolicyDomain = "WhyceID";

    public IdentityPolicyEnforcementAdapter(
        IdentityRegistry registry,
        IdentityRoleStore roleStore,
        IdentityTrustStore trustStore,
        IdentityRevocationStore revocationStore,
        IdentityAttributeStore attributeStore,
        IdentityDeviceStore deviceStore,
        IdentitySessionStore sessionStore)
    {
        _registry = registry;
        _roleStore = roleStore;
        _trustStore = trustStore;
        _revocationStore = revocationStore;
        _attributeStore = attributeStore;
        _deviceStore = deviceStore;
        _sessionStore = sessionStore;
    }

    public IdentityPolicyContext BuildContext(
        Guid identityId,
        string requestedOperation,
        string requestSource)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        var identity = _registry.Get(identityId);
        var roles = _roleStore.GetRoles(identityId);
        var trustScore = _trustStore.Get(identityId);
        var attributes = _attributeStore.Get(identityId);
        var devices = _deviceStore.Get(identityId);
        var sessions = _sessionStore.GetByIdentity(identityId);

        var attributeDict = attributes.ToDictionary(a => a.Key, a => a.Value);
        var trustedDeviceCount = devices.Count(d => d.Trusted);
        var activeSessionCount = sessions.Count(s => s.Active);

        var status = _revocationStore.IsRevoked(identityId)
            ? IdentityStatus.Revoked
            : identity.Status;

        return new IdentityPolicyContext(
            identityId,
            status,
            trustScore?.Score ?? 0,
            roles,
            attributeDict,
            trustedDeviceCount,
            activeSessionCount,
            requestedOperation,
            requestSource,
            DateTime.UtcNow
        );
    }

    public IdentityPolicyRequest CreateRequest(
        IdentityPolicyContext context,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new IdentityPolicyRequest(
            PolicyDomain,
            context.RequestedOperation,
            context,
            metadata ?? new Dictionary<string, string>()
        );
    }

    public IdentityPolicyDecision TranslateDecision(PolicyDecision policyDecision)
    {
        var action = policyDecision.Action switch
        {
            "Allow" => IdentityPolicyAction.Allow,
            "Deny" => IdentityPolicyAction.Deny,
            "RequireAdditionalVerification" => IdentityPolicyAction.RequireAdditionalVerification,
            "EscalateGovernanceReview" => IdentityPolicyAction.EscalateGovernanceReview,
            _ => IdentityPolicyAction.Deny
        };

        return new IdentityPolicyDecision(
            policyDecision.PolicyId,
            policyDecision.Allowed,
            action,
            policyDecision.Reason,
            policyDecision.EvaluatedAt
        );
    }

    public IdentityPolicyDecision Enforce(
        Guid identityId,
        string operation,
        string requestSource,
        Func<PolicyContext, PolicyDecision> policyEvaluator)
    {
        var context = BuildContext(identityId, operation, requestSource);
        var request = CreateRequest(context);

        var policyContext = new PolicyContext(
            Guid.NewGuid(),
            identityId,
            request.PolicyDomain,
            BuildPolicyAttributes(context),
            context.RequestTimestamp
        );

        var policyDecision = policyEvaluator(policyContext);
        return TranslateDecision(policyDecision);
    }

    private static IReadOnlyDictionary<string, string> BuildPolicyAttributes(
        IdentityPolicyContext context)
    {
        var attributes = new Dictionary<string, string>
        {
            ["identityStatus"] = context.IdentityStatus.ToString(),
            ["trustScore"] = context.TrustScore.ToString(),
            ["roleCount"] = context.Roles.Count.ToString(),
            ["deviceTrustLevel"] = context.DeviceTrustLevel.ToString(),
            ["sessionTrustLevel"] = context.SessionTrustLevel.ToString(),
            ["requestedOperation"] = context.RequestedOperation,
            ["requestSource"] = context.RequestSource
        };

        foreach (var role in context.Roles)
        {
            attributes[$"role.{role}"] = "true";
        }

        foreach (var attr in context.Attributes)
        {
            attributes[$"attr.{attr.Key}"] = attr.Value;
        }

        return attributes;
    }
}
