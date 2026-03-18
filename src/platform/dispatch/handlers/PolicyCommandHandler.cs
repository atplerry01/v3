namespace Whycespace.Platform.Dispatch.Handlers;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Safeguards;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Authority;
using Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;
using Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;
using Whycespace.Engines.T0U.WhycePolicy.Governance.DomainBinding;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Models;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Cache;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Parser;
using Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Models;
using Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Rollout;
using Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Versioning;
using Whycespace.Engines.T0U.WhycePolicy.Monitoring.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Monitoring.Audit;
using Whycespace.Engines.T0U.WhycePolicy.Monitoring.Evidence;
using Whycespace.Engines.T0U.WhycePolicy.Registry.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Models;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Forecasting;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyCommandHandler
{
    private readonly PolicyRegistryStore _policyRegistryStore;
    private readonly PolicyVersionStore _policyVersionStore;
    private readonly PolicyDependencyStore _policyDependencyStore;
    private readonly PolicyContextStore _policyContextStore;
    private readonly PolicyDecisionCacheStore _policyDecisionCacheStore;
    private readonly PolicyLifecycleStore _policyLifecycleStore;
    private readonly PolicyRolloutStore _policyRolloutStore;
    private readonly GovernanceAuthorityStore _governanceAuthorityStore;
    private readonly ConstitutionalPolicyStore _constitutionalPolicyStore;
    private readonly PolicyDomainBindingStore _policyDomainBindingStore;
    private readonly PolicyMonitoringStore _policyMonitoringStore;
    private readonly PolicyEvidenceStore _policyEvidenceStore;

    public PolicyCommandHandler(
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
        PolicyEvidenceStore policyEvidenceStore)
    {
        _policyRegistryStore = policyRegistryStore;
        _policyVersionStore = policyVersionStore;
        _policyDependencyStore = policyDependencyStore;
        _policyContextStore = policyContextStore;
        _policyDecisionCacheStore = policyDecisionCacheStore;
        _policyLifecycleStore = policyLifecycleStore;
        _policyRolloutStore = policyRolloutStore;
        _governanceAuthorityStore = governanceAuthorityStore;
        _constitutionalPolicyStore = constitutionalPolicyStore;
        _policyDomainBindingStore = policyDomainBindingStore;
        _policyMonitoringStore = policyMonitoringStore;
        _policyEvidenceStore = policyEvidenceStore;
    }

    public bool CanHandle(string command) => command.StartsWith("policy.");

    public Task<DispatchResult> HandleAsync(string command, Dictionary<string, object> payload)
    {
        return command switch
        {
            "policy.dsl.parse" => HandleDslParse(payload),
            "policy.registry.list" => HandleRegistryList(),
            "policy.registry.get" => HandleRegistryGet(payload),
            "policy.version.list" => HandleVersionList(payload),
            "policy.dependency.get" => HandleDependencyGet(payload),
            "policy.evaluate" => HandleEvaluate(payload),
            "policy.context.build" => HandleContextBuild(payload),
            "policy.cache.get" => HandleCacheGet(),
            "policy.cache.clear" => HandleCacheClear(),
            "policy.simulate" => HandleSimulate(payload),
            "policy.conflict.detect" => HandleConflictDetect(payload),
            "policy.forecast" => HandleForecast(payload),
            "policy.lifecycle.approve" => HandleLifecycleApprove(payload),
            "policy.lifecycle.activate" => HandleLifecycleActivate(payload),
            "policy.lifecycle.deprecate" => HandleLifecycleDeprecate(payload),
            "policy.lifecycle.archive" => HandleLifecycleArchive(payload),
            "policy.lifecycle.get" => HandleLifecycleGet(payload),
            "policy.rollout.check" => HandleRolloutCheck(payload),
            "policy.governance.assign" => HandleGovernanceAssign(payload),
            "policy.governance.get" => HandleGovernanceGet(payload),
            "policy.governance.check" => HandleGovernanceCheck(payload),
            "policy.constitutional.register" => HandleConstitutionalRegister(payload),
            "policy.enforce" => HandleEnforce(payload),
            "policy.domain.bind" => HandleDomainBind(payload),
            "policy.domain.getDomains" => HandleDomainGetDomains(payload),
            "policy.domain.getPolicies" => HandleDomainGetPolicies(payload),
            "policy.monitoring.getAll" => HandleMonitoringGetAll(),
            "policy.monitoring.get" => HandleMonitoringGet(payload),
            "policy.evidence.getAll" => HandleEvidenceGetAll(),
            "policy.evidence.get" => HandleEvidenceGet(payload),
            "policy.evidence.record" => HandleEvidenceRecord(payload),
            "policy.audit" => HandleAudit(payload),
            _ => Task.FromResult(DispatchResult.Fail($"Unknown policy command: '{command}'."))
        };
    }

    private Task<DispatchResult> HandleDslParse(Dictionary<string, object> payload)
    {
        var dsl = (string)payload["dsl"];
        var engine = new PolicyDslParserEngine();
        var definition = engine.Parse(dsl);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = definition.PolicyId,
            ["name"] = definition.Name,
            ["version"] = definition.Version,
            ["targetDomain"] = definition.TargetDomain,
            ["conditions"] = definition.Conditions.Select(c => new Dictionary<string, object>
            {
                ["field"] = c.Field,
                ["operator"] = c.Operator,
                ["value"] = c.Value
            }).ToList(),
            ["actions"] = definition.Actions.Select(a => new Dictionary<string, object>
            {
                ["actionType"] = a.ActionType,
                ["parameters"] = a.Parameters
            }).ToList(),
            ["createdAt"] = definition.CreatedAt
        }));
    }

    private Task<DispatchResult> HandleRegistryList()
    {
        var engine = new PolicyRegistryEngine(_policyRegistryStore);
        var policies = engine.GetPolicies();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policies"] = policies.Select(p => new Dictionary<string, object>
            {
                ["policyId"] = p.PolicyId,
                ["version"] = p.Version,
                ["name"] = p.PolicyDefinition.Name,
                ["targetDomain"] = p.PolicyDefinition.TargetDomain,
                ["status"] = p.Status.ToString(),
                ["registeredAt"] = p.RegisteredAt
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleRegistryGet(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var engine = new PolicyRegistryEngine(_policyRegistryStore);
        var record = engine.GetPolicy(policyId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["name"] = record.PolicyDefinition.Name,
            ["targetDomain"] = record.PolicyDefinition.TargetDomain,
            ["conditions"] = record.PolicyDefinition.Conditions,
            ["actions"] = record.PolicyDefinition.Actions,
            ["status"] = record.Status.ToString(),
            ["registeredAt"] = record.RegisteredAt
        }));
    }

    private Task<DispatchResult> HandleVersionList(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var engine = new PolicyVersionEngine(_policyVersionStore);
        var versions = engine.GetVersions(policyId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = policyId,
            ["versions"] = versions.Select(v => new Dictionary<string, object>
            {
                ["version"] = v.Version,
                ["status"] = v.Status.ToString(),
                ["createdAt"] = v.CreatedAt
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleDependencyGet(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var engine = new PolicyDependencyEngine(_policyDependencyStore);
        var dependencies = engine.GetDependencies(policyId);
        var resolvedOrder = engine.ResolveDependencyGraph(policyId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = policyId,
            ["dependencies"] = dependencies,
            ["resolvedOrder"] = resolvedOrder
        }));
    }

    private Task<DispatchResult> HandleEvaluate(Dictionary<string, object> payload)
    {
        var actorId = (Guid)payload["actorId"];
        var domain = (string)payload["domain"];
        var attributes = CastToDictionaryStringString(payload["attributes"]);

        var evaluationEngine = new PolicyEvaluationEngine(_policyRegistryStore, _policyDependencyStore);
        var contextEngine = new PolicyContextEngine(_policyContextStore);
        var context = contextEngine.BuildContext(actorId, domain, attributes);
        var decisions = evaluationEngine.EvaluatePolicies(domain, context);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["decisions"] = decisions.Select(d => new Dictionary<string, object>
            {
                ["policyId"] = d.PolicyId,
                ["allowed"] = d.Allowed,
                ["action"] = d.Action,
                ["reason"] = d.Reason,
                ["evaluatedAt"] = d.EvaluatedAt
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleContextBuild(Dictionary<string, object> payload)
    {
        var actorId = (Guid)payload["actorId"];
        var targetDomain = (string)payload["targetDomain"];
        var attributes = CastToDictionaryStringString(payload["attributes"]);

        var engine = new PolicyContextEngine(_policyContextStore);
        var context = engine.BuildContext(actorId, targetDomain, attributes);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["contextId"] = context.ContextId,
            ["actorId"] = context.ActorId,
            ["targetDomain"] = context.TargetDomain,
            ["attributes"] = context.Attributes,
            ["timestamp"] = context.Timestamp
        }));
    }

    private Task<DispatchResult> HandleCacheGet()
    {
        var engine = new PolicyDecisionCacheEngine(_policyDecisionCacheStore);
        _policyDecisionCacheStore.ClearExpired();
        var entries = _policyDecisionCacheStore.GetAll();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["entries"] = entries.Select(e => new Dictionary<string, object>
            {
                ["cacheKey"] = e.CacheKey,
                ["decisions"] = e.Decisions,
                ["cachedAt"] = e.CachedAt,
                ["expiresAt"] = e.ExpiresAt
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleCacheClear()
    {
        _policyDecisionCacheStore.Clear();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["message"] = "Policy decision cache cleared."
        }));
    }

    private Task<DispatchResult> HandleSimulate(Dictionary<string, object> payload)
    {
        var domain = (string)payload["domain"];
        var actorId = (string)payload["actorId"];
        var attributes = CastToDictionaryStringString(payload["attributes"]);

        var engine = new PolicySimulationEngine(_policyRegistryStore, _policyDependencyStore);
        var request = new PolicySimulationRequest(domain, actorId, attributes);
        var result = engine.SimulatePolicyEvaluation(request);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["domain"] = result.Domain,
            ["actorId"] = result.ActorId,
            ["decisions"] = result.Decisions,
            ["simulatedAt"] = result.SimulatedAt
        }));
    }

    private Task<DispatchResult> HandleConflictDetect(Dictionary<string, object> payload)
    {
        var domain = (string)payload["domain"];
        var engine = new PolicyConflictDetectionEngine(_policyRegistryStore, _policyDependencyStore);
        var report = engine.DetectConflicts(domain);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["domain"] = report.Domain,
            ["conflicts"] = report.Conflicts.Select(c => new Dictionary<string, object>
            {
                ["policyA"] = c.PolicyA,
                ["policyB"] = c.PolicyB,
                ["domain"] = c.Domain,
                ["reason"] = c.Reason,
                ["detectedAt"] = c.DetectedAt
            }).ToList(),
            ["generatedAt"] = report.GeneratedAt
        }));
    }

    private Task<DispatchResult> HandleForecast(Dictionary<string, object> payload)
    {
        var domain = (string)payload["domain"];
        var simulationContexts = (IReadOnlyList<PolicySimulationRequest>)payload["simulationContexts"];

        var engine = new PolicyImpactForecastEngine(_policyRegistryStore, _policyDependencyStore);
        var request = new PolicyImpactForecastRequest(domain, simulationContexts);
        var forecast = engine.ForecastImpact(request);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = forecast.PolicyId,
            ["domain"] = forecast.Domain,
            ["simulatedContexts"] = forecast.SimulatedContexts,
            ["allowedCount"] = forecast.AllowedCount,
            ["deniedCount"] = forecast.DeniedCount,
            ["loggedCount"] = forecast.LoggedCount,
            ["generatedAt"] = forecast.GeneratedAt
        }));
    }

    private Task<DispatchResult> HandleLifecycleApprove(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];

        var manager = new PolicyLifecycleManager(_policyLifecycleStore);
        var record = manager.ApprovePolicy(policyId, version);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["state"] = record.State.ToString(),
            ["updatedAt"] = record.UpdatedAt
        }));
    }

    private Task<DispatchResult> HandleLifecycleActivate(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];

        var manager = new PolicyLifecycleManager(_policyLifecycleStore);
        var record = manager.ActivatePolicy(policyId, version);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["state"] = record.State.ToString(),
            ["updatedAt"] = record.UpdatedAt
        }));
    }

    private Task<DispatchResult> HandleLifecycleDeprecate(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];

        var manager = new PolicyLifecycleManager(_policyLifecycleStore);
        var record = manager.DeprecatePolicy(policyId, version);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["state"] = record.State.ToString(),
            ["updatedAt"] = record.UpdatedAt
        }));
    }

    private Task<DispatchResult> HandleLifecycleArchive(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];

        var manager = new PolicyLifecycleManager(_policyLifecycleStore);
        var record = manager.ArchivePolicy(policyId, version);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["state"] = record.State.ToString(),
            ["updatedAt"] = record.UpdatedAt
        }));
    }

    private Task<DispatchResult> HandleLifecycleGet(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];

        var manager = new PolicyLifecycleManager(_policyLifecycleStore);
        var state = manager.GetLifecycleState(policyId, version);
        var history = manager.GetLifecycleHistory(policyId, version);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = state.PolicyId,
            ["version"] = state.Version,
            ["currentState"] = state.State.ToString(),
            ["updatedAt"] = state.UpdatedAt,
            ["history"] = history
        }));
    }

    private Task<DispatchResult> HandleRolloutCheck(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];
        var actorId = (string)payload["actorId"];
        var domain = (string)payload["domain"];

        var engine = new PolicyRolloutEngine(_policyRolloutStore);
        var active = engine.IsPolicyActiveForActor(policyId, version, actorId, domain);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["active"] = active
        }));
    }

    private Task<DispatchResult> HandleGovernanceAssign(Dictionary<string, object> payload)
    {
        var actorId = (string)payload["actorId"];
        var role = (string)payload["role"];

        var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
        var record = engine.AssignAuthority(actorId, Enum.Parse<GovernanceRole>(role));

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["actorId"] = record.ActorId,
            ["role"] = record.Role.ToString(),
            ["assignedAt"] = record.AssignedAt
        }));
    }

    private Task<DispatchResult> HandleGovernanceGet(Dictionary<string, object> payload)
    {
        var actorId = (string)payload["actorId"];

        var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
        var roles = engine.GetRoles(actorId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["actorId"] = actorId,
            ["roles"] = roles
        }));
    }

    private Task<DispatchResult> HandleGovernanceCheck(Dictionary<string, object> payload)
    {
        var actorId = (string)payload["actorId"];
        var role = (string)payload["role"];

        var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
        var hasAuthority = engine.HasAuthority(actorId, Enum.Parse<GovernanceRole>(role));

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["hasAuthority"] = hasAuthority
        }));
    }

    private Task<DispatchResult> HandleConstitutionalRegister(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];
        var protectionLevel = (string)payload["protectionLevel"];

        var engine = new ConstitutionalSafeguardEngine(_constitutionalPolicyStore);
        var record = engine.RegisterConstitutionalPolicy(policyId, version, protectionLevel);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = record.PolicyId,
            ["version"] = record.Version,
            ["protectionLevel"] = record.ProtectionLevel,
            ["registeredAt"] = record.RegisteredAt
        }));
    }

    private Task<DispatchResult> HandleEnforce(Dictionary<string, object> payload)
    {
        var actorId = (string)payload["actorId"];
        var domain = (string)payload["domain"];
        var operation = (string)payload["operation"];
        var attributes = CastToDictionaryStringString(payload["attributes"]);

        var evaluationEngine = new PolicyEvaluationEngine(_policyRegistryStore, _policyDependencyStore);
        var contextEngine = new PolicyContextEngine(_policyContextStore);
        var cacheEngine = new PolicyDecisionCacheEngine(_policyDecisionCacheStore);
        var enforcementEngine = new PolicyEnforcementEngine(evaluationEngine, contextEngine, cacheEngine);

        var request = new PolicyEnforcementRequest(actorId, domain, operation, attributes);
        var result = enforcementEngine.EnforcePolicy(request);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["allowed"] = result.Allowed,
            ["reason"] = result.Reason,
            ["decisions"] = result.Decisions,
            ["evaluatedAt"] = result.EvaluatedAt
        }));
    }

    private Task<DispatchResult> HandleDomainBind(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var version = (string)payload["version"];
        var domain = (string)payload["domain"];

        var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
        var binding = engine.BindPolicy(policyId, version, domain);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = binding.PolicyId,
            ["version"] = binding.Version,
            ["domain"] = binding.Domain,
            ["boundAt"] = binding.BoundAt
        }));
    }

    private Task<DispatchResult> HandleDomainGetDomains(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];

        var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
        var domains = engine.GetDomainsForPolicy(policyId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = policyId,
            ["domains"] = domains
        }));
    }

    private Task<DispatchResult> HandleDomainGetPolicies(Dictionary<string, object> payload)
    {
        var domain = (string)payload["domain"];

        var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
        var policies = engine.GetPoliciesForDomain(domain);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["domain"] = domain,
            ["policies"] = policies
        }));
    }

    private Task<DispatchResult> HandleMonitoringGetAll()
    {
        var engine = new PolicyMonitoringEngine(_policyMonitoringStore);
        var metrics = engine.GetAllMetrics();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["metrics"] = metrics
        }));
    }

    private Task<DispatchResult> HandleMonitoringGet(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];

        var engine = new PolicyMonitoringEngine(_policyMonitoringStore);
        var metrics = engine.GetPolicyMetrics(policyId);

        if (metrics is null)
            return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>()));

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = metrics.PolicyId,
            ["domain"] = metrics.Domain,
            ["evaluations"] = metrics.Evaluations,
            ["allowedCount"] = metrics.AllowedCount,
            ["deniedCount"] = metrics.DeniedCount,
            ["lastEvaluatedAt"] = metrics.LastEvaluatedAt
        }));
    }

    private Task<DispatchResult> HandleEvidenceGetAll()
    {
        var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
        var evidence = engine.GetAllEvidence();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["evidence"] = evidence
        }));
    }

    private Task<DispatchResult> HandleEvidenceGet(Dictionary<string, object> payload)
    {
        var evidenceId = (string)payload["evidenceId"];

        var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
        var evidence = engine.GetEvidence(evidenceId);

        if (evidence is null)
            return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>()));

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["evidenceId"] = evidence.EvidenceId,
            ["policyId"] = evidence.PolicyId,
            ["actorId"] = evidence.ActorId,
            ["domain"] = evidence.Domain,
            ["operation"] = evidence.Operation,
            ["allowed"] = evidence.Allowed,
            ["reason"] = evidence.Reason,
            ["recordedAt"] = evidence.RecordedAt
        }));
    }

    private Task<DispatchResult> HandleEvidenceRecord(Dictionary<string, object> payload)
    {
        var policyId = (string)payload["policyId"];
        var actorId = (string)payload["actorId"];
        var domain = (string)payload["domain"];
        var operation = (string)payload["operation"];
        var allowed = (bool)payload["allowed"];
        var reason = (string)payload["reason"];

        var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
        var record = engine.RecordPolicyEvidence(policyId, actorId, domain, operation, allowed, reason);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["evidenceId"] = record.EvidenceId,
            ["policyId"] = record.PolicyId,
            ["actorId"] = record.ActorId,
            ["domain"] = record.Domain,
            ["operation"] = record.Operation,
            ["allowed"] = record.Allowed,
            ["reason"] = record.Reason,
            ["recordedAt"] = record.RecordedAt
        }));
    }

    private Task<DispatchResult> HandleAudit(Dictionary<string, object> payload)
    {
        var policyId = payload.TryGetValue("policyId", out var pid) ? (string?)pid : null;
        var actorId = payload.TryGetValue("actorId", out var aid) ? (string?)aid : null;
        var domain = payload.TryGetValue("domain", out var dom) ? (string?)dom : null;
        var from = payload.TryGetValue("from", out var f) ? (DateTime?)f : null;
        var to = payload.TryGetValue("to", out var t) ? (DateTime?)t : null;

        var engine = new PolicyAuditEngine(_policyEvidenceStore);
        var query = new PolicyAuditQuery(policyId, actorId, domain, from, to);
        var report = engine.AuditPolicy(query);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["evidenceRecords"] = report.EvidenceRecords,
            ["totalRecords"] = report.TotalRecords,
            ["generatedAt"] = report.GeneratedAt
        }));
    }

    private static Dictionary<string, string> CastToDictionaryStringString(object value)
    {
        if (value is Dictionary<string, string> already)
            return already;

        if (value is Dictionary<string, object> dict)
            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);

        throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Dictionary<string, string>.");
    }
}
