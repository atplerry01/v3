using Adapters = Whycespace.Systems.WhyceID.Adapters;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using SystemAdapter = Whycespace.Systems.WhyceID.Adapters.IdentityPolicyEnforcementAdapter;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityPolicyEnforcementAdapterTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityRevocationStore _revocationStore;
    private readonly IdentityAttributeStore _attributeStore;
    private readonly IdentityDeviceStore _deviceStore;
    private readonly IdentitySessionStore _sessionStore;
    private readonly SystemAdapter _adapter;

    public IdentityPolicyEnforcementAdapterTests()
    {
        _registry = new IdentityRegistry();
        _roleStore = new IdentityRoleStore();
        _trustStore = new IdentityTrustStore();
        _revocationStore = new IdentityRevocationStore();
        _attributeStore = new IdentityAttributeStore();
        _deviceStore = new IdentityDeviceStore();
        _sessionStore = new IdentitySessionStore();
        _adapter = new SystemAdapter(
            _registry, _roleStore, _trustStore, _revocationStore,
            _attributeStore, _deviceStore, _sessionStore);
    }

    private Guid RegisterVerifiedIdentity(int trustScore = 80)
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        identity.Verify();
        _registry.Register(identity);
        _trustStore.Update(id.Value, new IdentityTrustScore(trustScore, DateTime.UtcNow));
        return id.Value;
    }

    [Fact]
    public void Enforce_PolicyAllowDecision_ReturnsAllowed()
    {
        var identityId = RegisterVerifiedIdentity(80);

        var result = _adapter.Enforce(
            identityId,
            Adapters.IdentityPolicyOperations.CreateIdentity,
            "test",
            _ => new PolicyDecision("policy-1", true, "Allow", "Identity meets requirements", DateTime.UtcNow));

        Assert.True(result.Allowed);
        Assert.Equal(Adapters.IdentityPolicyAction.Allow, result.Action);
        Assert.Equal("policy-1", result.PolicyId);
    }

    [Fact]
    public void Enforce_PolicyDenyDecision_ReturnsDenied()
    {
        var identityId = RegisterVerifiedIdentity(80);

        var result = _adapter.Enforce(
            identityId,
            Adapters.IdentityPolicyOperations.AssignRole,
            "test",
            _ => new PolicyDecision("policy-2", false, "Deny", "Role assignment blocked", DateTime.UtcNow));

        Assert.False(result.Allowed);
        Assert.Equal(Adapters.IdentityPolicyAction.Deny, result.Action);
        Assert.Equal("Role assignment blocked", result.Reason);
    }

    [Fact]
    public void Enforce_PolicyRequiresAdditionalVerification_ReturnsCorrectAction()
    {
        var identityId = RegisterVerifiedIdentity(60);

        var result = _adapter.Enforce(
            identityId,
            Adapters.IdentityPolicyOperations.RegisterDevice,
            "test",
            _ => new PolicyDecision("policy-3", false, "RequireAdditionalVerification",
                "Additional verification needed", DateTime.UtcNow));

        Assert.False(result.Allowed);
        Assert.Equal(Adapters.IdentityPolicyAction.RequireAdditionalVerification, result.Action);
    }

    [Fact]
    public void Enforce_GovernanceEscalation_ReturnsEscalateAction()
    {
        var identityId = RegisterVerifiedIdentity(80);

        var result = _adapter.Enforce(
            identityId,
            Adapters.IdentityPolicyOperations.RevokeIdentity,
            "test",
            _ => new PolicyDecision("policy-4", false, "EscalateGovernanceReview",
                "Governance review required", DateTime.UtcNow));

        Assert.False(result.Allowed);
        Assert.Equal(Adapters.IdentityPolicyAction.EscalateGovernanceReview, result.Action);
        Assert.Equal("Governance review required", result.Reason);
    }

    [Fact]
    public void BuildContext_ConstructsCorrectContext()
    {
        var identityId = RegisterVerifiedIdentity(75);
        _roleStore.Assign(identityId, "admin");
        _roleStore.Assign(identityId, "operator");
        _attributeStore.Add(identityId, new IdentityAttribute("department", "engineering", DateTime.UtcNow));
        _deviceStore.Register(identityId, new IdentityDevice(Guid.NewGuid(), "fp-1", true, DateTime.UtcNow));

        var context = _adapter.BuildContext(identityId, "CreateIdentity", "api");

        Assert.Equal(identityId, context.IdentityId);
        Assert.Equal(IdentityStatus.Verified, context.IdentityStatus);
        Assert.Equal(75, context.TrustScore);
        Assert.Equal(2, context.Roles.Count);
        Assert.True(context.Attributes.ContainsKey("department"));
        Assert.Equal(1, context.DeviceTrustLevel);
        Assert.Equal("CreateIdentity", context.RequestedOperation);
        Assert.Equal("api", context.RequestSource);
        Assert.Equal(IdentityStatus.Verified, context.IdentityStatus);
    }

    [Fact]
    public void BuildContext_RevokedIdentity_ReturnsRevokedStatus()
    {
        var identityId = RegisterVerifiedIdentity(80);
        _revocationStore.Register(new IdentityRevocation(
            Guid.NewGuid(), identityId, "security_breach", DateTime.UtcNow, true));

        var context = _adapter.BuildContext(identityId, "AssignRole", "test");

        Assert.Equal(IdentityStatus.Revoked, context.IdentityStatus);
    }

    [Fact]
    public void BuildContext_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _adapter.BuildContext(missingId, "CreateIdentity", "test"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void CreateRequest_ConstructsCorrectRequest()
    {
        var identityId = RegisterVerifiedIdentity(80);
        var context = _adapter.BuildContext(identityId, "GrantPermission", "api");
        var metadata = new Dictionary<string, string> { ["source"] = "admin-console" };

        var request = _adapter.CreateRequest(context, metadata);

        Assert.Equal("WhyceID", request.PolicyDomain);
        Assert.Equal("GrantPermission", request.Operation);
        Assert.Same(context, request.IdentityContext);
        Assert.Equal("admin-console", request.Metadata["source"]);
    }

    [Fact]
    public void TranslateDecision_MapsAllActions()
    {
        var allow = _adapter.TranslateDecision(new PolicyDecision("p1", true, "Allow", "ok", DateTime.UtcNow));
        Assert.Equal(Adapters.IdentityPolicyAction.Allow, allow.Action);

        var deny = _adapter.TranslateDecision(new PolicyDecision("p2", false, "Deny", "no", DateTime.UtcNow));
        Assert.Equal(Adapters.IdentityPolicyAction.Deny, deny.Action);

        var verify = _adapter.TranslateDecision(new PolicyDecision("p3", false, "RequireAdditionalVerification", "need more", DateTime.UtcNow));
        Assert.Equal(Adapters.IdentityPolicyAction.RequireAdditionalVerification, verify.Action);

        var escalate = _adapter.TranslateDecision(new PolicyDecision("p4", false, "EscalateGovernanceReview", "review", DateTime.UtcNow));
        Assert.Equal(Adapters.IdentityPolicyAction.EscalateGovernanceReview, escalate.Action);
    }

    [Fact]
    public void TranslateDecision_UnknownAction_DefaultsToDeny()
    {
        var result = _adapter.TranslateDecision(
            new PolicyDecision("p1", false, "UnknownAction", "unknown", DateTime.UtcNow));

        Assert.Equal(Adapters.IdentityPolicyAction.Deny, result.Action);
    }

    [Fact]
    public void Enforce_PassesCorrectAttributesToPolicyEvaluator()
    {
        var identityId = RegisterVerifiedIdentity(90);
        _roleStore.Assign(identityId, "guardian");
        PolicyContext? capturedContext = null;

        _adapter.Enforce(
            identityId,
            Adapters.IdentityPolicyOperations.RecoverIdentity,
            "recovery-service",
            ctx =>
            {
                capturedContext = ctx;
                return new PolicyDecision("p1", true, "Allow", "ok", DateTime.UtcNow);
            });

        Assert.NotNull(capturedContext);
        Assert.Equal(identityId, capturedContext!.ActorId);
        Assert.Equal("WhyceID", capturedContext.TargetDomain);
        Assert.Equal("Verified", capturedContext.Attributes["identityStatus"]);
        Assert.Equal("90", capturedContext.Attributes["trustScore"]);
        Assert.Equal("RecoverIdentity", capturedContext.Attributes["requestedOperation"]);
        Assert.Equal("true", capturedContext.Attributes["role.guardian"]);
    }
}
