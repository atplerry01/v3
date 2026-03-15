namespace Whycespace.Runtime.PlatformDispatch;

using Whycespace.Contracts.Runtime;
using Whycespace.Runtime.PlatformDispatch.Handlers;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;
using Whycespace.System.Upstream.WhycePolicy.Stores;
using Whycespace.System.Upstream.Governance.Stores;

/// <summary>
/// Factory that wires up the PlatformDispatcher with all engine command handlers.
/// This is the single point where the runtime layer connects to engines,
/// keeping the platform layer free of engine dependencies.
/// </summary>
public static class PlatformDispatcherFactory
{
    public static IPlatformDispatcher Create(
        // Identity stores
        IdentityRegistry identityRegistry,
        IdentityAttributeStore identityAttributeStore,
        IdentityRoleStore identityRoleStore,
        IdentityPermissionStore identityPermissionStore,
        IdentityAccessScopeStore identityAccessScopeStore,
        IdentityTrustStore identityTrustStore,
        IdentityDeviceStore identityDeviceStore,
        IdentitySessionStore identitySessionStore,
        IdentityConsentStore identityConsentStore,
        IdentityGraphStore identityGraphStore,
        IdentityServiceStore identityServiceStore,
        IdentityFederationStore identityFederationStore,
        IdentityRecoveryStore identityRecoveryStore,
        IdentityRevocationStore identityRevocationStore,
        IdentityAuditStore identityAuditStore,
        // Policy stores
        PolicyRegistryStore policyRegistryStore,
        PolicyVersionStore policyVersionStore,
        PolicyDependencyStore policyDependencyStore,
        PolicyContextStore policyContextStore,
        PolicyDecisionCacheStore policyDecisionCacheStore,
        PolicyLifecycleStore policyLifecycleStore,
        PolicyRolloutStore policyRolloutStore,
        GovernanceAuthorityStore governanceAuthorityStore,
        ConstitutionalPolicyStore constitutionalPolicyStore,
        PolicyDomainBindingStore policyDomainBindingStore,
        PolicyMonitoringStore policyMonitoringStore,
        PolicyEvidenceStore policyEvidenceStore,
        // Governance stores
        GuardianRegistryStore guardianRegistryStore,
        GovernanceRoleStore governanceRoleStore,
        GovernanceDelegationStore governanceDelegationStore,
        // WSS bootstrapper
        WssRuntimeBootstrapper wssBootstrapper)
    {
        var identityHandler = new IdentityCommandHandler(
            identityRegistry,
            identityAttributeStore,
            identityRoleStore,
            identityPermissionStore,
            identityAccessScopeStore,
            identityTrustStore,
            identityDeviceStore,
            identitySessionStore,
            identityConsentStore,
            identityGraphStore,
            identityServiceStore,
            identityFederationStore,
            identityRecoveryStore,
            identityRevocationStore,
            identityAuditStore);

        var policyHandler = new PolicyCommandHandler(
            policyRegistryStore,
            policyVersionStore,
            policyDependencyStore,
            policyContextStore,
            policyDecisionCacheStore,
            policyLifecycleStore,
            policyRolloutStore,
            governanceAuthorityStore,
            constitutionalPolicyStore,
            policyDomainBindingStore,
            policyMonitoringStore,
            policyEvidenceStore);

        var governanceHandler = new GovernanceCommandHandler(
            guardianRegistryStore,
            governanceRoleStore,
            governanceDelegationStore,
            identityRegistry);

        var wssHandler = new WssCommandHandler(
            wssBootstrapper.WorkflowDefinitionStore,
            wssBootstrapper.WorkflowTemplateStore,
            wssBootstrapper.WorkflowRegistryStore,
            wssBootstrapper.WorkflowVersionStore,
            wssBootstrapper.EngineMappingStore,
            wssBootstrapper.InstanceRegistryStore,
            wssBootstrapper.WorkflowStateStore,
            wssBootstrapper.RetryStore,
            wssBootstrapper.TimeoutStore,
            wssBootstrapper.WorkflowRegistry,
            wssBootstrapper.WorkflowEventRouter,
            wssBootstrapper.RetryPolicyEngine,
            wssBootstrapper.TimeoutEngine,
            wssBootstrapper.LifecycleEngine);

        return new PlatformDispatcher(
            identityHandler,
            policyHandler,
            governanceHandler,
            wssHandler);
    }
}
