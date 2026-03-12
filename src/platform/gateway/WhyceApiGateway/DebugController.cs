namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.ArchitectureGuardrails.Enforcement;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Events;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.Domain.Clusters;
using Whycespace.Domain.Core.Cluster;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Services;
using Whycespace.ClusterTemplatePlatform;
using Whycespace.Domain.Core.Economic;
using Whycespace.RuntimeValidation.Runners;
using Whycespace.RuntimeValidation.Pipelines;
using Whycespace.EngineManifest.Registry;
using Whycespace.WorkerPoolRuntime.Pool;
using Whycespace.WorkerPoolRuntime.Queue;
using Whycespace.EventFabricRuntime.Routing;
using Whycespace.EventFabricRuntime.Registry;
using Whycespace.ProjectionRuntime.Registry;
using Whycespace.ProjectionRuntime.Storage;
using ProjectionRuntimeRegistry = Whycespace.ProjectionRuntime.Registry.ProjectionRegistry;
using Whycespace.ReliabilityRuntime.Retry;
using Whycespace.ReliabilityRuntime.Dlq;
using Whycespace.ReliabilityRuntime.Timeout;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;
using Whycespace.System.WhyceID.Models;
using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhycePolicy.Stores;
using Whycespace.System.Upstream.WhyceChain.Stores;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.WSS.Validation;
using Whycespace.Engines.T1M.WSS.Dependency;
using Whycespace.Engines.T1M.WSS.Mapping;
using Whycespace.Engines.T1M.WSS.Instance;
using Whycespace.System.Midstream.WSS.Models;
using WorkflowStateStore = Whycespace.Runtime.Workflow.WorkflowStateStore;

[ApiController]
[Route("dev")]
public sealed class DebugController : ControllerBase
{
    private readonly WorkflowStateStore _stateStore;
    private readonly EngineRegistry _engineRegistry;
    private readonly EventBus _eventBus;
    private readonly WorkflowMapper _workflowMapper;
    private readonly ClusterBootstrapper _clusterBootstrapper;
    private readonly SimulationService _simulationService;
    private readonly ClusterTemplateService _clusterTemplateService;
    private readonly SpvEconomicRegistry _spvEconomicRegistry;
    private readonly ValidationRunner _validationRunner;
    private readonly EngineManifestRegistry _manifestRegistry;
    private readonly EngineWorkerPoolManager _workerPoolManager;
    private readonly EngineExecutionQueue _executionQueue;
    private readonly EventTopicRouter _eventTopicRouter;
    private readonly EventSubscriptionRegistry _eventSubscriptionRegistry;
    private readonly ProjectionRuntimeRegistry _projectionRuntimeRegistry;
    private readonly ProjectionStateStore _projectionStateStore;
    private readonly RetryPolicyManager _retryPolicyManager;
    private readonly DeadLetterQueueManager _deadLetterQueueManager;
    private readonly WorkflowTimeoutManager _workflowTimeoutManager;
    private readonly IdentityRegistry _identityRegistry;
    private readonly IdentityAttributeStore _identityAttributeStore;
    private readonly IdentityRoleStore _identityRoleStore;
    private readonly IdentityPermissionStore _identityPermissionStore;
    private readonly IdentityAccessScopeStore _identityAccessScopeStore;
    private readonly IdentityTrustStore _identityTrustStore;
    private readonly IdentityDeviceStore _identityDeviceStore;
    private readonly IdentitySessionStore _identitySessionStore;
    private readonly IdentityConsentStore _identityConsentStore;
    private readonly IdentityGraphStore _identityGraphStore;
    private readonly IdentityServiceStore _identityServiceStore;
    private readonly IdentityFederationStore _identityFederationStore;
    private readonly IdentityRecoveryStore _identityRecoveryStore;
    private readonly IdentityRevocationStore _identityRevocationStore;
    private readonly IdentityAuditStore _identityAuditStore;
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
    private readonly ChainLedgerStore _chainLedgerStore;
    private readonly ChainBlockStore _chainBlockStore;
    private readonly ChainEventStore _chainEventStore;
    private readonly GuardianRegistryStore _guardianRegistryStore;
    private readonly GovernanceRoleStore _governanceRoleStore;
    private readonly WorkflowDefinitionStore _workflowDefinitionStore;
    private readonly WorkflowTemplateStore _workflowTemplateStore;
    private readonly WorkflowRegistryStore _workflowRegistryStore;
    private readonly WorkflowVersionStore _workflowVersionStore;
    private readonly WorkflowRegistry _workflowRegistry;
    private readonly WorkflowEngineMappingStore _engineMappingStore;
    private readonly WorkflowInstanceRegistryStore _instanceRegistryStore;
    private readonly WssWorkflowStateStore _wssWorkflowStateStore;

    public DebugController(
        WorkflowStateStore stateStore,
        EngineRegistry engineRegistry,
        EventBus eventBus,
        WorkflowMapper workflowMapper,
        ClusterBootstrapper clusterBootstrapper,
        SimulationService simulationService,
        ClusterTemplateService clusterTemplateService,
        SpvEconomicRegistry spvEconomicRegistry,
        ValidationRunner validationRunner,
        EngineManifestRegistry manifestRegistry,
        EngineWorkerPoolManager workerPoolManager,
        EngineExecutionQueue executionQueue,
        EventTopicRouter eventTopicRouter,
        EventSubscriptionRegistry eventSubscriptionRegistry,
        ProjectionRuntimeRegistry projectionRuntimeRegistry,
        ProjectionStateStore projectionStateStore,
        RetryPolicyManager retryPolicyManager,
        DeadLetterQueueManager deadLetterQueueManager,
        WorkflowTimeoutManager workflowTimeoutManager,
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
        ChainLedgerStore chainLedgerStore,
        ChainBlockStore chainBlockStore,
        ChainEventStore chainEventStore,
        GuardianRegistryStore guardianRegistryStore,
        GovernanceRoleStore governanceRoleStore,
        WorkflowDefinitionStore workflowDefinitionStore,
        WorkflowTemplateStore workflowTemplateStore,
        WorkflowRegistryStore workflowRegistryStore,
        WorkflowVersionStore workflowVersionStore,
        WorkflowRegistry workflowRegistry,
        WorkflowEngineMappingStore engineMappingStore,
        WorkflowInstanceRegistryStore instanceRegistryStore,
        WssWorkflowStateStore wssWorkflowStateStore)
    {
        _stateStore = stateStore;
        _engineRegistry = engineRegistry;
        _eventBus = eventBus;
        _workflowMapper = workflowMapper;
        _clusterBootstrapper = clusterBootstrapper;
        _simulationService = simulationService;
        _clusterTemplateService = clusterTemplateService;
        _spvEconomicRegistry = spvEconomicRegistry;
        _validationRunner = validationRunner;
        _manifestRegistry = manifestRegistry;
        _workerPoolManager = workerPoolManager;
        _executionQueue = executionQueue;
        _eventTopicRouter = eventTopicRouter;
        _eventSubscriptionRegistry = eventSubscriptionRegistry;
        _projectionRuntimeRegistry = projectionRuntimeRegistry;
        _projectionStateStore = projectionStateStore;
        _retryPolicyManager = retryPolicyManager;
        _deadLetterQueueManager = deadLetterQueueManager;
        _workflowTimeoutManager = workflowTimeoutManager;
        _identityRegistry = identityRegistry;
        _identityAttributeStore = identityAttributeStore;
        _identityRoleStore = identityRoleStore;
        _identityPermissionStore = identityPermissionStore;
        _identityAccessScopeStore = identityAccessScopeStore;
        _identityTrustStore = identityTrustStore;
        _identityDeviceStore = identityDeviceStore;
        _identitySessionStore = identitySessionStore;
        _identityConsentStore = identityConsentStore;
        _identityGraphStore = identityGraphStore;
        _identityServiceStore = identityServiceStore;
        _identityFederationStore = identityFederationStore;
        _identityRecoveryStore = identityRecoveryStore;
        _identityRevocationStore = identityRevocationStore;
        _identityAuditStore = identityAuditStore;
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
        _chainLedgerStore = chainLedgerStore;
        _chainBlockStore = chainBlockStore;
        _chainEventStore = chainEventStore;
        _guardianRegistryStore = guardianRegistryStore;
        _governanceRoleStore = governanceRoleStore;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowTemplateStore = workflowTemplateStore;
        _workflowRegistryStore = workflowRegistryStore;
        _workflowVersionStore = workflowVersionStore;
        _workflowRegistry = workflowRegistry;
        _engineMappingStore = engineMappingStore;
        _instanceRegistryStore = instanceRegistryStore;
        _wssWorkflowStateStore = wssWorkflowStateStore;
    }

    [HttpGet("workflows")]
    public IActionResult GetWorkflows() => Ok(_stateStore.GetAll());

    [HttpGet("engines")]
    public IActionResult GetEngines() => Ok(_engineRegistry.GetRegisteredEngines());

    [HttpGet("projections")]
    public IActionResult GetProjections() => Ok(new[] { "DriverLocation", "PropertyListing", "VaultBalance", "Revenue" });

    [HttpGet("events")]
    public IActionResult GetEvents() => Ok(_eventBus.GetPublishedEvents());

    [HttpPost("workflow/run")]
    public async Task<IActionResult> RunWorkflow([FromBody] DebugRunWorkflowDto dto)
    {
        var definition = _workflowMapper.Resolve(dto.WorkflowName);
        if (definition is null) return NotFound($"Workflow not found: {dto.WorkflowName}");
        return Ok(new { message = $"Workflow {dto.WorkflowName} queued", registeredWorkflows = _workflowMapper.GetRegisteredWorkflows() });
    }

    [HttpPost("event/replay")]
    public async Task<IActionResult> ReplayEvent([FromBody] DebugReplayEventDto dto)
    {
        var @event = SystemEvent.Create(dto.EventType, dto.AggregateId, dto.Payload);
        await _eventBus.PublishAsync(@event);
        return Ok(new { message = "Event replayed", eventId = @event.EventId });
    }

    [HttpGet("guardrails/rules")]
    public IActionResult GetGuardrailRules()
    {
        return Ok(new { rules = ArchitectureRules.Names });
    }

    [HttpGet("clusters")]
    public IActionResult GetClusters()
    {
        var clusters = _clusterBootstrapper.Administration.GetAllClusters()
            .Select(c => c.ClusterName)
            .ToList();
        return Ok(new { clusters });
    }

    [HttpGet("clusters/subclusters")]
    public IActionResult GetSubClusters()
    {
        var result = _clusterBootstrapper.Administration.GetAllClusters()
            .ToDictionary(
                c => c.ClusterName,
                c => c.SubClusters.Select(s => s.SubClusterName).ToList());
        return Ok(result);
    }

    [HttpGet("clusters/providers")]
    public IActionResult GetClusterProviders()
    {
        var providers = _clusterBootstrapper.ProviderRegistry.GetProviders()
            .Select(p => new { p.ProviderId, p.ProviderName, p.ProviderType, p.ClusterId })
            .ToList();
        return Ok(new { providers });
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _clusterBootstrapper.ProviderRegistry.GetProviders()
            .Select(p => p.ProviderName)
            .ToList();
        return Ok(new { providers });
    }

    [HttpGet("providers/assignments")]
    public IActionResult GetProviderAssignments()
    {
        var assignments = _clusterBootstrapper.AssignmentService.GetAllAssignments();
        var registry = _clusterBootstrapper.ProviderRegistry;

        var result = assignments.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Select(id => registry.GetProvider(id)?.ProviderName ?? id.ToString())
                .ToList());

        return Ok(result);
    }

    [HttpGet("guardrails/validate")]
    public IActionResult ValidateGuardrails()
    {
        var enforcement = new GuardrailEnforcementEngine();
        var engineAssembly = typeof(Whycespace.Engines.T2E.Clusters.Mobility.Taxi.RideExecutionEngine).Assembly;
        var sharedAssembly = typeof(Whycespace.Contracts.Engines.IEngine).Assembly;

        var report = enforcement.Validate(engineAssembly, sharedAssembly);

        if (report.IsValid)
            return Ok(new { status = "valid" });

        return Ok(new
        {
            status = "invalid",
            violations = report.AllViolations
        });
    }

    [HttpPost("simulation/run")]
    public IActionResult RunSimulation([FromBody] DebugRunSimulationDto dto)
    {
        var result = _simulationService.RunScenario(dto.ScenarioId);
        return Ok(new { result.ScenarioId, result.ProjectedRevenue, result.ProjectedAssets, result.ProjectedProfit });
    }

    [HttpGet("simulation/scenarios")]
    public IActionResult GetSimulationScenarios()
    {
        return Ok(_simulationService.GetScenarios());
    }

    [HttpGet("simulation/results")]
    public IActionResult GetSimulationResults()
    {
        return Ok(_simulationService.GetResults());
    }

    [HttpGet("cluster-templates")]
    public IActionResult GetClusterTemplates()
    {
        var templates = _clusterTemplateService.Registry.ListTemplates();
        return Ok(new { templates });
    }

    [HttpPost("cluster-templates/generate")]
    public IActionResult GenerateClusterFromTemplate([FromBody] DebugGenerateClusterDto dto)
    {
        var result = _clusterTemplateService.Generator.GenerateCluster(dto.TemplateName);
        return Ok(new { cluster = result.Cluster, subclusters = result.SubClusters });
    }

    [HttpGet("economic/spvs")]
    public IActionResult GetEconomicSpvs()
    {
        var spvs = _spvEconomicRegistry.ListSpvs()
            .Select(s => new { s.SpvId, s.ClusterName, s.SubClusterName })
            .ToList();
        return Ok(new { spvs });
    }

    [HttpGet("economic/revenue")]
    public IActionResult GetEconomicRevenue()
    {
        return Ok(new { metrics = "Revenue metrics derived from event projections" });
    }

    [HttpGet("economic/profits")]
    public IActionResult GetEconomicProfits()
    {
        return Ok(new { metrics = "Profit distribution metrics derived from event projections" });
    }

    [HttpGet("platform/routes")]
    public IActionResult GetPlatformRoutes()
    {
        return Ok(new
        {
            routes = new[]
            {
                "/api/commands/ride/request",
                "/api/commands/property/list",
                "/api/operator/engines",
                "/api/operator/invocations",
                "/api/operator/deadletters",
                "/api/operator/clusters",
                "/api/queries/projections/{name}",
                "/api/queries/projections"
            }
        });
    }

    [HttpGet("platform/tools")]
    public IActionResult GetPlatformTools()
    {
        return Ok(new
        {
            tools = new[]
            {
                "workflow-inspector",
                "engine-inspector",
                "event-replayer",
                "projection-query",
                "context-dump",
                "pipeline-trace",
                "workflow-simulator"
            }
        });
    }

    [HttpGet("platform/health")]
    public IActionResult GetPlatformHealth()
    {
        var engines = _engineRegistry.GetRegisteredEngines();
        return Ok(new
        {
            status = "healthy",
            registeredEngines = engines.Count,
            publishedEvents = _eventBus.GetPublishedEvents().Count,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("validation/run")]
    public async Task<IActionResult> RunValidation()
    {
        var reports = await _validationRunner.RunAllAsync();
        return Ok(new
        {
            status = "validation complete",
            scenarios = reports.Select(r => new { name = r.ScenarioName, r.Success, r.ExecutionTime, r.Steps, r.Errors })
        });
    }

    [HttpGet("validation/results")]
    public IActionResult GetValidationResults()
    {
        var reports = _validationRunner.GetResults();
        return Ok(new
        {
            scenarios = reports.Select(r => new { name = r.ScenarioName, r.Success })
        });
    }

    [HttpGet("validation/pipeline")]
    public IActionResult GetPipelineStatus()
    {
        var status = PipelineStatus.Healthy();
        return Ok(new
        {
            api = status.Api,
            commands = status.Commands,
            workflows = status.Workflows,
            engines = status.Engines,
            events = status.Events,
            projections = status.Projections
        });
    }

    [HttpGet("runtime/engine-manifests")]
    public IActionResult GetEngineManifests()
    {
        var manifests = _manifestRegistry.GetAll()
            .Select(m => new
            {
                engineName = m.EngineName,
                engineType = m.EngineType,
                inputContract = m.InputContract,
                outputContract = m.OutputContract,
                capabilities = m.Capabilities.Select(c => c.Name).ToList(),
                version = m.Version
            })
            .ToList();

        return Ok(manifests);
    }

    [HttpGet("runtime/worker-pool")]
    public IActionResult GetWorkerPool()
    {
        return Ok(new
        {
            workers = _workerPoolManager.GetWorkers().Count,
            queueSize = _executionQueue.Count()
        });
    }

    [HttpGet("runtime/projections")]
    public IActionResult GetProjectionRuntime()
    {
        return Ok(new
        {
            projections = _projectionRuntimeRegistry.GetMappings(),
            records = _projectionStateStore.GetAll().Count
        });
    }

    [HttpGet("runtime/reliability")]
    public IActionResult GetReliabilityRuntime()
    {
        return Ok(new
        {
            retryAttempts = 0,
            deadLetterRecords = _deadLetterQueueManager.GetAll().Count,
            trackedWorkflows = _workflowTimeoutManager.TrackedCount
        });
    }

    [HttpGet("runtime/event-fabric")]
    public IActionResult GetEventFabric()
    {
        return Ok(new
        {
            routes = _eventTopicRouter.GetRoutes(),
            subscriptions = _eventSubscriptionRegistry.GetSubscriptionCounts()
        });
    }

    [HttpGet("identity/count")]
    public IActionResult GetIdentityCount()
    {
        return Ok(new { count = _identityRegistry.Count });
    }

    [HttpGet("identity/{id:guid}")]
    public IActionResult GetIdentity(Guid id)
    {
        try
        {
            var identity = _identityRegistry.Get(id);
            return Ok(new
            {
                identityId = identity.Id.Value,
                type = identity.Type.ToString(),
                status = identity.Status.ToString(),
                createdAt = identity.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Identity not found" });
        }
    }

    [HttpGet("identity/{id:guid}/attributes")]
    public IActionResult GetIdentityAttributes(Guid id)
    {
        var attributes = _identityAttributeStore.Get(id);
        return Ok(new
        {
            identityId = id,
            attributes = attributes.Select(a => new
            {
                key = a.Key,
                value = a.Value,
                createdAt = a.CreatedAt
            })
        });
    }

    [HttpGet("identity/{id:guid}/roles")]
    public IActionResult GetIdentityRoles(Guid id)
    {
        var roles = _identityRoleStore.GetRoles(id);
        return Ok(new
        {
            identityId = id,
            roles
        });
    }

    [HttpPost("identity/{id:guid}/verify")]
    public IActionResult VerifyIdentity(Guid id)
    {
        try
        {
            var identity = _identityRegistry.Get(id);
            identity.Verify();
            _identityRegistry.Update(identity);
            return Ok(new
            {
                identityId = id,
                status = identity.Status.ToString(),
                verifiedAt = identity.VerifiedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Identity not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("identity/{id:guid}/trustscore")]
    public IActionResult CalculateTrustScore(Guid id)
    {
        try
        {
            var engine = new Whycespace.Engines.T0U.WhyceID.TrustScoreEngine(
                _identityRegistry, _identityTrustStore);
            var result = engine.Calculate(id);
            return Ok(new
            {
                identityId = id,
                score = result.Score,
                calculatedAt = result.CalculatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("identity/{id:guid}/devices")]
    public IActionResult GetIdentityDevices(Guid id)
    {
        var devices = _identityDeviceStore.Get(id);
        return Ok(new
        {
            identityId = id,
            devices = devices.Select(d => new
            {
                deviceId = d.DeviceId,
                fingerprint = d.Fingerprint,
                trusted = d.Trusted,
                registeredAt = d.RegisteredAt
            })
        });
    }

    [HttpPost("authenticate")]
    public IActionResult Authenticate([FromBody] DebugAuthenticateDto dto)
    {
        var trustEngine = new Whycespace.Engines.T0U.WhyceID.TrustScoreEngine(
            _identityRegistry, _identityTrustStore);
        var deviceEngine = new Whycespace.Engines.T0U.WhyceID.DeviceTrustEngine(
            _identityRegistry, _identityDeviceStore);
        var authEngine = new Whycespace.Engines.T0U.WhyceID.AuthenticationEngine(
            _identityRegistry, trustEngine, deviceEngine);

        var result = authEngine.Authenticate(dto.IdentityId, dto.DeviceId);
        return Ok(new
        {
            success = result.Success,
            message = result.Message
        });
    }

    [HttpPost("authorize")]
    public IActionResult Authorize([FromBody] DebugAuthorizeDto dto)
    {
        var roleEngine = new Whycespace.Engines.T0U.WhyceID.IdentityRoleEngine(
            _identityRegistry, _identityRoleStore);
        var permissionEngine = new Whycespace.Engines.T0U.WhyceID.IdentityPermissionEngine(
            _identityPermissionStore);
        var scopeEngine = new Whycespace.Engines.T0U.WhyceID.IdentityAccessScopeEngine(
            _identityAccessScopeStore);
        var authzEngine = new Whycespace.Engines.T0U.WhyceID.AuthorizationEngine(
            _identityRegistry, roleEngine, permissionEngine, scopeEngine);

        var result = authzEngine.Authorize(new Whycespace.System.WhyceID.Models.AuthorizationRequest(
            dto.IdentityId, dto.Resource, dto.Action, dto.Scope));
        return Ok(new
        {
            allowed = result.Allowed,
            reason = result.Reason
        });
    }

    [HttpGet("identity/{id:guid}/sessions")]
    public IActionResult GetIdentitySessions(Guid id)
    {
        var sessions = _identitySessionStore.GetByIdentity(id);
        return Ok(new
        {
            identityId = id,
            sessions = sessions.Select(s => new
            {
                sessionId = s.SessionId,
                deviceId = s.DeviceId,
                createdAt = s.CreatedAt,
                expiresAt = s.ExpiresAt,
                active = s.Active
            })
        });
    }

    [HttpPost("session/revoke")]
    public IActionResult RevokeSession([FromBody] DebugRevokeSessionDto dto)
    {
        _identitySessionStore.Revoke(dto.SessionId);
        return Ok(new { message = "Session revoked", sessionId = dto.SessionId });
    }

    [HttpGet("identity/{id:guid}/consents")]
    public IActionResult GetIdentityConsents(Guid id)
    {
        var consents = _identityConsentStore.GetByIdentity(id);
        return Ok(new
        {
            identityId = id,
            consents = consents.Select(c => new
            {
                consentId = c.ConsentId,
                target = c.Target,
                scope = c.Scope,
                grantedAt = c.GrantedAt,
                revoked = c.Revoked
            })
        });
    }

    [HttpPost("identity/{id:guid}/consent")]
    public IActionResult GrantConsent(Guid id, [FromBody] DebugGrantConsentDto dto)
    {
        try
        {
            var engine = new Whycespace.Engines.T0U.WhyceID.ConsentEngine(
                _identityRegistry, _identityConsentStore);
            var consent = engine.GrantConsent(id, dto.Target, dto.Scope);
            return Ok(new
            {
                consentId = consent.ConsentId,
                identityId = consent.IdentityId,
                target = consent.Target,
                scope = consent.Scope,
                grantedAt = consent.GrantedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("consent/revoke")]
    public IActionResult RevokeConsent([FromBody] DebugRevokeConsentDto dto)
    {
        _identityConsentStore.Revoke(dto.ConsentId);
        return Ok(new { message = "Consent revoked", consentId = dto.ConsentId });
    }

    [HttpGet("roles/{role}/permissions")]
    public IActionResult GetRolePermissions(string role)
    {
        var permissions = _identityPermissionStore.GetPermissions(role);
        return Ok(new
        {
            role,
            permissions
        });
    }

    [HttpGet("roles/{role}/scopes")]
    public IActionResult GetRoleScopes(string role)
    {
        var scopes = _identityAccessScopeStore.GetScopes(role);
        return Ok(new
        {
            role,
            scopes
        });
    }

    [HttpGet("identity/registry")]
    public IActionResult GetIdentityRegistry()
    {
        var all = _identityRegistry.GetAll();
        return Ok(new
        {
            totalCount = _identityRegistry.Count,
            identities = all.Select(i => new
            {
                identityId = i.Id.Value,
                status = i.Status.ToString()
            })
        });
    }

    [HttpGet("identity/{id:guid}/relationships")]
    public IActionResult GetIdentityRelationships(Guid id)
    {
        var engine = new IdentityGraphEngine(_identityRegistry, _identityGraphStore);
        var relationships = engine.GetRelationships(id);
        return Ok(new
        {
            identityId = id,
            relationships = relationships.Select(e => new
            {
                edgeId = e.EdgeId,
                targetEntityId = e.TargetEntityId,
                relationship = e.Relationship,
                createdAt = e.CreatedAt
            })
        });
    }

    [HttpPost("identity/{id:guid}/relationship")]
    public IActionResult CreateRelationship(Guid id, [FromBody] DebugCreateRelationshipDto dto)
    {
        try
        {
            var engine = new IdentityGraphEngine(_identityRegistry, _identityGraphStore);
            var edge = engine.CreateRelationship(id, dto.TargetEntityId, dto.Relationship);
            return Ok(new
            {
                edgeId = edge.EdgeId,
                sourceIdentityId = edge.SourceIdentityId,
                targetEntityId = edge.TargetEntityId,
                relationship = edge.Relationship,
                createdAt = edge.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("relationship/remove")]
    public IActionResult RemoveRelationship([FromBody] DebugRemoveRelationshipDto dto)
    {
        _identityGraphStore.Remove(dto.EdgeId);
        return Ok(new { message = "Relationship removed", edgeId = dto.EdgeId });
    }

    [HttpGet("services")]
    public IActionResult GetServices()
    {
        var engine = new ServiceIdentityEngine(_identityServiceStore);
        var services = engine.GetServices();
        return Ok(new
        {
            services = services.Select(s => new
            {
                serviceId = s.ServiceId,
                name = s.Name,
                type = s.Type,
                createdAt = s.CreatedAt,
                revoked = s.Revoked
            })
        });
    }

    [HttpPost("services/register")]
    public IActionResult RegisterService([FromBody] DebugRegisterServiceDto dto)
    {
        try
        {
            var engine = new ServiceIdentityEngine(_identityServiceStore);
            var service = engine.RegisterService(dto.Name, dto.Type, dto.Secret);
            return Ok(new
            {
                serviceId = service.ServiceId,
                name = service.Name,
                type = service.Type,
                createdAt = service.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("services/revoke")]
    public IActionResult RevokeService([FromBody] DebugRevokeServiceDto dto)
    {
        _identityServiceStore.Revoke(dto.ServiceId);
        return Ok(new { message = "Service revoked", serviceId = dto.ServiceId });
    }

    [HttpGet("federations")]
    public IActionResult GetFederations()
    {
        var engine = new FederationEngine(_identityRegistry, _identityFederationStore);
        var federations = engine.GetFederations();
        return Ok(new
        {
            federations = federations.Select(f => new
            {
                federationId = f.FederationId,
                provider = f.Provider,
                externalIdentityId = f.ExternalIdentityId,
                internalIdentityId = f.InternalIdentityId,
                createdAt = f.CreatedAt,
                revoked = f.Revoked
            })
        });
    }

    [HttpPost("federation/register")]
    public IActionResult RegisterFederation([FromBody] DebugRegisterFederationDto dto)
    {
        try
        {
            var engine = new FederationEngine(_identityRegistry, _identityFederationStore);
            var federation = engine.RegisterFederation(dto.Provider, dto.ExternalIdentityId, dto.InternalIdentityId);
            return Ok(new
            {
                federationId = federation.FederationId,
                provider = federation.Provider,
                externalIdentityId = federation.ExternalIdentityId,
                internalIdentityId = federation.InternalIdentityId,
                createdAt = federation.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("federation/revoke")]
    public IActionResult RevokeFederation([FromBody] DebugRevokeFederationDto dto)
    {
        _identityFederationStore.Revoke(dto.FederationId);
        return Ok(new { message = "Federation revoked", federationId = dto.FederationId });
    }

    [HttpGet("identity/{id:guid}/recoveries")]
    public IActionResult GetIdentityRecoveries(Guid id)
    {
        var engine = new IdentityRecoveryEngine(_identityRegistry, _identityRecoveryStore);
        var recoveries = engine.GetRecoveries(id);
        return Ok(new
        {
            identityId = id,
            recoveries = recoveries.Select(r => new
            {
                recoveryId = r.RecoveryId,
                reason = r.Reason,
                status = r.Status,
                createdAt = r.CreatedAt,
                completedAt = r.CompletedAt
            })
        });
    }

    [HttpPost("recovery/create")]
    public IActionResult CreateRecovery([FromBody] DebugCreateRecoveryDto dto)
    {
        try
        {
            var engine = new IdentityRecoveryEngine(_identityRegistry, _identityRecoveryStore);
            var recovery = engine.CreateRecovery(dto.IdentityId, dto.Reason);
            return Ok(new
            {
                recoveryId = recovery.RecoveryId,
                identityId = recovery.IdentityId,
                reason = recovery.Reason,
                status = recovery.Status,
                createdAt = recovery.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("recovery/approve")]
    public IActionResult ApproveRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        try
        {
            var engine = new IdentityRecoveryEngine(_identityRegistry, _identityRecoveryStore);
            engine.ApproveRecovery(dto.RecoveryId);
            return Ok(new { message = "Recovery approved", recoveryId = dto.RecoveryId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Recovery not found" });
        }
    }

    [HttpPost("recovery/reject")]
    public IActionResult RejectRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        try
        {
            var engine = new IdentityRecoveryEngine(_identityRegistry, _identityRecoveryStore);
            engine.RejectRecovery(dto.RecoveryId);
            return Ok(new { message = "Recovery rejected", recoveryId = dto.RecoveryId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Recovery not found" });
        }
    }

    [HttpPost("recovery/complete")]
    public IActionResult CompleteRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        try
        {
            var engine = new IdentityRecoveryEngine(_identityRegistry, _identityRecoveryStore);
            engine.CompleteRecovery(dto.RecoveryId);
            return Ok(new { message = "Recovery completed", recoveryId = dto.RecoveryId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Recovery not found" });
        }
    }

    [HttpGet("identity/{id:guid}/revocations")]
    public IActionResult GetIdentityRevocations(Guid id)
    {
        var engine = new IdentityRevocationEngine(_identityRegistry, _identityRevocationStore);
        var revocations = engine.GetRevocations(id);
        return Ok(new
        {
            identityId = id,
            revocations = revocations.Select(r => new
            {
                revocationId = r.RevocationId,
                reason = r.Reason,
                createdAt = r.CreatedAt,
                active = r.Active
            })
        });
    }

    [HttpPost("identity/revoke")]
    public IActionResult RevokeIdentity([FromBody] DebugRevokeIdentityDto dto)
    {
        try
        {
            var engine = new IdentityRevocationEngine(_identityRegistry, _identityRevocationStore);
            var revocation = engine.RevokeIdentity(dto.IdentityId, dto.Reason);
            return Ok(new
            {
                revocationId = revocation.RevocationId,
                identityId = revocation.IdentityId,
                reason = revocation.Reason,
                createdAt = revocation.CreatedAt,
                active = revocation.Active
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("revocations")]
    public IActionResult GetAllRevocations()
    {
        var engine = new IdentityRevocationEngine(_identityRegistry, _identityRevocationStore);
        var revocations = engine.GetAllRevocations();
        return Ok(new
        {
            revocations = revocations.Select(r => new
            {
                revocationId = r.RevocationId,
                identityId = r.IdentityId,
                reason = r.Reason,
                createdAt = r.CreatedAt,
                active = r.Active
            })
        });
    }

    [HttpPost("identity/policy/evaluate")]
    public IActionResult EvaluateIdentityPolicy([FromBody] DebugEvaluatePolicyDto dto)
    {
        try
        {
            var adapter = new IdentityPolicyEnforcementAdapter(
                _identityRegistry, _identityRoleStore, _identityTrustStore, _identityRevocationStore);
            var context = adapter.BuildContext(dto.IdentityId);
            var allowed = adapter.EvaluateIdentityAccess(dto.IdentityId);
            return Ok(new
            {
                identityId = dto.IdentityId,
                allowed,
                context = new
                {
                    roles = context.Roles,
                    trustScore = context.TrustScore,
                    verified = context.Verified,
                    revoked = context.Revoked
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("identity/{id:guid}/audit")]
    public IActionResult GetIdentityAudit(Guid id)
    {
        var engine = new IdentityAuditEngine(_identityRegistry, _identityAuditStore);
        var events = engine.GetIdentityAudit(id);
        return Ok(new
        {
            identityId = id,
            events = events.Select(e => new
            {
                eventId = e.EventId,
                eventType = e.EventType,
                description = e.Description,
                timestamp = e.Timestamp
            })
        });
    }

    [HttpPost("identity/audit")]
    public IActionResult RecordAuditEvent([FromBody] DebugRecordAuditDto dto)
    {
        try
        {
            var engine = new IdentityAuditEngine(_identityRegistry, _identityAuditStore);
            var auditEvent = engine.RecordEvent(dto.IdentityId, dto.EventType, dto.Description);
            return Ok(new
            {
                eventId = auditEvent.EventId,
                identityId = auditEvent.IdentityId,
                eventType = auditEvent.EventType,
                description = auditEvent.Description,
                timestamp = auditEvent.Timestamp
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("policy/parse")]
    public IActionResult ParsePolicyDsl([FromBody] DebugParsePolicyDto dto)
    {
        try
        {
            var engine = new PolicyDslParserEngine();
            var result = engine.Parse(dto.Dsl);
            return Ok(new
            {
                policyId = result.PolicyId,
                name = result.Name,
                version = result.Version,
                targetDomain = result.TargetDomain,
                conditions = result.Conditions.Select(c => new
                {
                    field = c.Field,
                    @operator = c.Operator,
                    value = c.Value
                }),
                actions = result.Actions.Select(a => new
                {
                    actionType = a.ActionType,
                    parameters = a.Parameters
                }),
                createdAt = result.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("policies")]
    public IActionResult GetPolicies()
    {
        var engine = new PolicyRegistryEngine(_policyRegistryStore);
        var policies = engine.GetPolicies();
        return Ok(new
        {
            policies = policies.Select(p => new
            {
                policyId = p.PolicyId,
                version = p.Version,
                name = p.PolicyDefinition.Name,
                targetDomain = p.PolicyDefinition.TargetDomain,
                status = p.Status.ToString(),
                registeredAt = p.RegisteredAt
            })
        });
    }

    [HttpGet("policies/{id}")]
    public IActionResult GetPolicy(string id)
    {
        try
        {
            var engine = new PolicyRegistryEngine(_policyRegistryStore);
            var record = engine.GetPolicy(id);
            return Ok(new
            {
                policyId = record.PolicyId,
                version = record.Version,
                name = record.PolicyDefinition.Name,
                targetDomain = record.PolicyDefinition.TargetDomain,
                conditions = record.PolicyDefinition.Conditions.Select(c => new
                {
                    field = c.Field,
                    @operator = c.Operator,
                    value = c.Value
                }),
                actions = record.PolicyDefinition.Actions.Select(a => new
                {
                    actionType = a.ActionType,
                    parameters = a.Parameters
                }),
                status = record.Status.ToString(),
                registeredAt = record.RegisteredAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Policy not found: '{id}'" });
        }
    }

    [HttpGet("policies/{id}/versions")]
    public IActionResult GetPolicyVersions(string id)
    {
        var engine = new PolicyVersionEngine(_policyVersionStore);
        var versions = engine.GetVersions(id);
        return Ok(new
        {
            policyId = id,
            versions = versions.Select(v => new
            {
                version = v.Version,
                status = v.Status.ToString(),
                createdAt = v.CreatedAt
            })
        });
    }

    [HttpGet("policies/{id}/dependencies")]
    public IActionResult GetPolicyDependencies(string id)
    {
        var engine = new PolicyDependencyEngine(_policyDependencyStore);
        var dependencies = engine.GetDependencies(id);
        var resolved = engine.ResolveDependencyGraph(id);
        return Ok(new
        {
            policyId = id,
            dependencies = dependencies.Select(d => d.DependsOnPolicyId),
            resolvedOrder = resolved
        });
    }

    [HttpPost("policy/evaluate")]
    public IActionResult EvaluatePolicies([FromBody] DebugEvaluatePoliciesDto dto)
    {
        try
        {
            var engine = new PolicyEvaluationEngine(_policyRegistryStore, _policyDependencyStore);
            var contextEngine = new PolicyContextEngine(_policyContextStore);
            var context = contextEngine.BuildContext(dto.ActorId, dto.Domain, dto.Attributes);
            var decisions = engine.EvaluatePolicies(dto.Domain, context);
            return Ok(new
            {
                decisions = decisions.Select(d => new
                {
                    policyId = d.PolicyId,
                    allowed = d.Allowed,
                    action = d.Action,
                    reason = d.Reason,
                    evaluatedAt = d.EvaluatedAt
                })
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("policy/context")]
    public IActionResult BuildPolicyContext([FromBody] DebugBuildPolicyContextDto dto)
    {
        try
        {
            var engine = new PolicyContextEngine(_policyContextStore);
            var result = engine.BuildContext(dto.ActorId, dto.TargetDomain, dto.Attributes);
            return Ok(new
            {
                contextId = result.ContextId,
                actorId = result.ActorId,
                targetDomain = result.TargetDomain,
                attributes = result.Attributes,
                timestamp = result.Timestamp
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("policy/cache")]
    public IActionResult GetPolicyCache()
    {
        var engine = new PolicyDecisionCacheEngine(_policyDecisionCacheStore);
        _policyDecisionCacheStore.ClearExpired();
        var entries = _policyDecisionCacheStore.GetAll();
        return Ok(new
        {
            entries = entries.Select(e => new
            {
                cacheKey = e.CacheKey,
                decisions = e.Decisions.Select(d => new
                {
                    policyId = d.PolicyId,
                    allowed = d.Allowed,
                    action = d.Action,
                    reason = d.Reason,
                    evaluatedAt = d.EvaluatedAt
                }),
                cachedAt = e.CachedAt,
                expiresAt = e.ExpiresAt
            })
        });
    }

    [HttpDelete("policy/cache")]
    public IActionResult ClearPolicyCache()
    {
        _policyDecisionCacheStore.Clear();
        return Ok(new { message = "Policy decision cache cleared." });
    }

    [HttpPost("policy/simulate")]
    public IActionResult SimulatePolicyEvaluation([FromBody] DebugSimulatePolicyDto dto)
    {
        try
        {
            var engine = new PolicySimulationEngine(_policyRegistryStore, _policyDependencyStore);
            var request = new Whycespace.System.Upstream.WhycePolicy.Models.PolicySimulationRequest(
                dto.Domain, dto.ActorId, dto.Attributes);
            var result = engine.SimulatePolicyEvaluation(request);
            return Ok(new
            {
                domain = result.Domain,
                actorId = result.ActorId,
                decisions = result.Decisions.Select(d => new
                {
                    policyId = d.PolicyId,
                    allowed = d.Allowed,
                    action = d.Action,
                    reason = d.Reason,
                    evaluatedAt = d.EvaluatedAt
                }),
                simulatedAt = result.SimulatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("policy/conflicts/{domain}")]
    public IActionResult DetectPolicyConflicts(string domain)
    {
        var engine = new PolicyConflictDetectionEngine(_policyRegistryStore, _policyDependencyStore);
        var report = engine.DetectConflicts(domain);
        return Ok(new
        {
            domain = report.Domain,
            conflicts = report.Conflicts.Select(c => new
            {
                policyA = c.PolicyA,
                policyB = c.PolicyB,
                domain = c.Domain,
                reason = c.Reason,
                detectedAt = c.DetectedAt
            }),
            generatedAt = report.GeneratedAt
        });
    }

    [HttpPost("policy/forecast")]
    public IActionResult ForecastPolicyImpact([FromBody] DebugForecastPolicyDto dto)
    {
        try
        {
            var engine = new PolicyImpactForecastEngine(_policyRegistryStore, _policyDependencyStore);
            var simContexts = dto.SimulationContexts
                .Select(s => new Whycespace.System.Upstream.WhycePolicy.Models.PolicySimulationRequest(s.Domain, s.ActorId, s.Attributes))
                .ToList();
            var request = new Whycespace.System.Upstream.WhycePolicy.Models.PolicyImpactForecastRequest(dto.Domain, simContexts);
            var forecast = engine.ForecastImpact(request);
            return Ok(new
            {
                policyId = forecast.PolicyId,
                domain = forecast.Domain,
                simulatedContexts = forecast.SimulatedContexts,
                allowedCount = forecast.AllowedCount,
                deniedCount = forecast.DeniedCount,
                loggedCount = forecast.LoggedCount,
                generatedAt = forecast.GeneratedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("policy/lifecycle/approve")]
    public IActionResult ApprovePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        try
        {
            var engine = new PolicyLifecycleManager(_policyLifecycleStore);
            var result = engine.ApprovePolicy(dto.PolicyId, dto.Version);
            return Ok(new { policyId = result.PolicyId, version = result.Version, state = result.State.ToString(), updatedAt = result.UpdatedAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("policy/lifecycle/activate")]
    public IActionResult ActivatePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        try
        {
            var engine = new PolicyLifecycleManager(_policyLifecycleStore);
            var result = engine.ActivatePolicy(dto.PolicyId, dto.Version);
            return Ok(new { policyId = result.PolicyId, version = result.Version, state = result.State.ToString(), updatedAt = result.UpdatedAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("policy/lifecycle/deprecate")]
    public IActionResult DeprecatePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        try
        {
            var engine = new PolicyLifecycleManager(_policyLifecycleStore);
            var result = engine.DeprecatePolicy(dto.PolicyId, dto.Version);
            return Ok(new { policyId = result.PolicyId, version = result.Version, state = result.State.ToString(), updatedAt = result.UpdatedAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("policy/lifecycle/archive")]
    public IActionResult ArchivePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        try
        {
            var engine = new PolicyLifecycleManager(_policyLifecycleStore);
            var result = engine.ArchivePolicy(dto.PolicyId, dto.Version);
            return Ok(new { policyId = result.PolicyId, version = result.Version, state = result.State.ToString(), updatedAt = result.UpdatedAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("policy/lifecycle/{policyId}/{version}")]
    public IActionResult GetPolicyLifecycle(string policyId, string version)
    {
        try
        {
            var engine = new PolicyLifecycleManager(_policyLifecycleStore);
            var state = engine.GetLifecycleState(policyId, version);
            var history = engine.GetLifecycleHistory(policyId, version);
            return Ok(new
            {
                policyId = state.PolicyId,
                version = state.Version,
                currentState = state.State.ToString(),
                updatedAt = state.UpdatedAt,
                history = history.Select(h => new
                {
                    state = h.State.ToString(),
                    updatedAt = h.UpdatedAt
                })
            });
        }
        catch (KeyNotFoundException) { return NotFound(new { message = $"No lifecycle state found for policy '{policyId}' version '{version}'." }); }
    }

    [HttpPost("policy/rollout")]
    public IActionResult SetPolicyRollout([FromBody] DebugSetRolloutDto dto)
    {
        var config = new Whycespace.System.Upstream.WhycePolicy.Models.PolicyRolloutConfig(
            dto.PolicyId, dto.Version,
            Enum.Parse<Whycespace.System.Upstream.WhycePolicy.Models.PolicyRolloutStrategy>(dto.Strategy, ignoreCase: true),
            dto.Percentage, dto.Actors ?? new List<string>(),
            dto.Domains ?? new List<string>(), DateTime.UtcNow);
        _policyRolloutStore.SetRolloutConfig(config);
        return Ok(new
        {
            policyId = config.PolicyId, version = config.Version,
            strategy = config.Strategy.ToString(), percentage = config.Percentage,
            actors = config.Actors, domains = config.Domains, createdAt = config.CreatedAt
        });
    }

    [HttpGet("policy/rollout/{policyId}/{version}")]
    public IActionResult GetPolicyRollout(string policyId, string version)
    {
        var config = _policyRolloutStore.GetRolloutConfig(policyId, version);
        if (config is null)
            return NotFound(new { message = $"No rollout config found for policy '{policyId}' version '{version}'." });
        return Ok(new
        {
            policyId = config.PolicyId, version = config.Version,
            strategy = config.Strategy.ToString(), percentage = config.Percentage,
            actors = config.Actors, domains = config.Domains, createdAt = config.CreatedAt
        });
    }

    [HttpPost("policy/rollout/check")]
    public IActionResult CheckPolicyRollout([FromBody] DebugCheckRolloutDto dto)
    {
        var engine = new PolicyRolloutEngine(_policyRolloutStore);
        var active = engine.IsPolicyActiveForActor(dto.PolicyId, dto.Version, dto.ActorId, dto.Domain);
        return Ok(new { active });
    }

    [HttpPost("policy/governance/assign")]
    public IActionResult AssignGovernanceAuthority([FromBody] DebugGovernanceAssignDto dto)
    {
        try
        {
            var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
            var role = Enum.Parse<Whycespace.System.Upstream.WhycePolicy.Models.GovernanceRole>(dto.Role, ignoreCase: true);
            var record = engine.AssignAuthority(dto.ActorId, role);
            return Ok(new { actorId = record.ActorId, role = record.Role.ToString(), assignedAt = record.AssignedAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("policy/governance/{actorId}")]
    public IActionResult GetGovernanceAuthority(string actorId)
    {
        var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
        var roles = engine.GetRoles(actorId);
        return Ok(new
        {
            actorId,
            roles = roles.Select(r => new { role = r.Role.ToString(), assignedAt = r.AssignedAt })
        });
    }

    [HttpPost("policy/governance/check")]
    public IActionResult CheckGovernanceAuthority([FromBody] DebugGovernanceAssignDto dto)
    {
        var engine = new GovernanceAuthorityEngine(_governanceAuthorityStore);
        var role = Enum.Parse<Whycespace.System.Upstream.WhycePolicy.Models.GovernanceRole>(dto.Role, ignoreCase: true);
        var hasAuthority = engine.HasAuthority(dto.ActorId, role);
        return Ok(new { hasAuthority });
    }

    [HttpPost("policy/constitutional/register")]
    public IActionResult RegisterConstitutionalPolicy([FromBody] DebugConstitutionalRegisterDto dto)
    {
        var engine = new ConstitutionalSafeguardEngine(_constitutionalPolicyStore);
        var record = engine.RegisterConstitutionalPolicy(dto.PolicyId, dto.Version, dto.ProtectionLevel);
        return Ok(new { policyId = record.PolicyId, version = record.Version, protectionLevel = record.ProtectionLevel, registeredAt = record.RegisteredAt });
    }

    [HttpGet("policy/constitutional/{policyId}/{version}")]
    public IActionResult GetConstitutionalPolicy(string policyId, string version)
    {
        var record = _constitutionalPolicyStore.Get(policyId, version);
        if (record is null)
            return NotFound(new { message = $"No constitutional protection found for policy '{policyId}' version '{version}'." });
        return Ok(new { policyId = record.PolicyId, version = record.Version, protectionLevel = record.ProtectionLevel, registeredAt = record.RegisteredAt });
    }

    [HttpPost("policy/constitutional/check")]
    public IActionResult CheckConstitutionalPolicy([FromBody] DebugConstitutionalCheckDto dto)
    {
        var isProtected = _constitutionalPolicyStore.IsProtectedPolicy(dto.PolicyId, dto.Version);
        var protectionLevel = _constitutionalPolicyStore.GetProtectionLevel(dto.PolicyId, dto.Version);
        return Ok(new { isProtected, protectionLevel });
    }

    [HttpPost("policy/enforce")]
    public IActionResult EnforcePolicy([FromBody] DebugEnforcePolicyDto dto)
    {
        try
        {
            var evaluationEngine = new PolicyEvaluationEngine(_policyRegistryStore, _policyDependencyStore);
            var contextEngine = new PolicyContextEngine(_policyContextStore);
            var cacheEngine = new PolicyDecisionCacheEngine(_policyDecisionCacheStore);
            var enforcementEngine = new PolicyEnforcementEngine(evaluationEngine, contextEngine, cacheEngine);

            var request = new Whycespace.System.Upstream.WhycePolicy.Models.PolicyEnforcementRequest(
                dto.ActorId, dto.Domain, dto.Operation, dto.Attributes);

            var result = enforcementEngine.EnforcePolicy(request);
            return Ok(new
            {
                allowed = result.Allowed,
                reason = result.Reason,
                decisions = result.Decisions.Select(d => new
                {
                    policyId = d.PolicyId,
                    allowed = d.Allowed,
                    action = d.Action,
                    reason = d.Reason,
                    evaluatedAt = d.EvaluatedAt
                }),
                evaluatedAt = result.EvaluatedAt
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("policy/domain/bind")]
    public IActionResult BindPolicyToDomain([FromBody] DebugBindPolicyDomainDto dto)
    {
        try
        {
            var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
            var binding = engine.BindPolicy(dto.PolicyId, dto.Version, dto.Domain);
            return Ok(new { policyId = binding.PolicyId, version = binding.Version, domain = binding.Domain, boundAt = binding.BoundAt });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("policy/domain/{policyId}")]
    public IActionResult GetDomainsForPolicy(string policyId)
    {
        var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
        var domains = engine.GetDomainsForPolicy(policyId);
        return Ok(new { policyId, domains });
    }

    [HttpGet("policy/domain/policies/{domain}")]
    public IActionResult GetPoliciesForDomain(string domain)
    {
        var engine = new PolicyDomainBindingEngine(_policyDomainBindingStore);
        var bindings = engine.GetPoliciesForDomain(domain);
        return Ok(new
        {
            domain,
            policies = bindings.Select(b => new
            {
                policyId = b.PolicyId,
                version = b.Version,
                boundAt = b.BoundAt
            })
        });
    }

    [HttpGet("policy/monitoring")]
    public IActionResult GetAllPolicyMonitoring()
    {
        var engine = new PolicyMonitoringEngine(_policyMonitoringStore);
        var metrics = engine.GetAllMetrics();
        return Ok(new
        {
            metrics = metrics.Select(m => new
            {
                policyId = m.PolicyId,
                domain = m.Domain,
                evaluations = m.Evaluations,
                allowedCount = m.AllowedCount,
                deniedCount = m.DeniedCount,
                lastEvaluatedAt = m.LastEvaluatedAt
            })
        });
    }

    [HttpGet("policy/monitoring/{policyId}")]
    public IActionResult GetPolicyMonitoring(string policyId)
    {
        var engine = new PolicyMonitoringEngine(_policyMonitoringStore);
        var metrics = engine.GetPolicyMetrics(policyId);
        if (metrics is null)
            return NotFound(new { message = $"No monitoring data found for policy '{policyId}'." });
        return Ok(new
        {
            policyId = metrics.PolicyId,
            domain = metrics.Domain,
            evaluations = metrics.Evaluations,
            allowedCount = metrics.AllowedCount,
            deniedCount = metrics.DeniedCount,
            lastEvaluatedAt = metrics.LastEvaluatedAt
        });
    }

    [HttpGet("policy/evidence")]
    public IActionResult GetAllPolicyEvidence()
    {
        var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
        var records = engine.GetAllEvidence();
        return Ok(new
        {
            evidence = records.Select(r => new
            {
                evidenceId = r.EvidenceId,
                policyId = r.PolicyId,
                actorId = r.ActorId,
                domain = r.Domain,
                operation = r.Operation,
                allowed = r.Allowed,
                reason = r.Reason,
                recordedAt = r.RecordedAt
            })
        });
    }

    [HttpGet("policy/evidence/{evidenceId}")]
    public IActionResult GetPolicyEvidence(string evidenceId)
    {
        var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
        var record = engine.GetEvidence(evidenceId);
        if (record is null)
            return NotFound(new { message = $"No evidence record found for '{evidenceId}'." });
        return Ok(new
        {
            evidenceId = record.EvidenceId,
            policyId = record.PolicyId,
            actorId = record.ActorId,
            domain = record.Domain,
            operation = record.Operation,
            allowed = record.Allowed,
            reason = record.Reason,
            recordedAt = record.RecordedAt
        });
    }

    [HttpPost("policy/evidence")]
    public IActionResult RecordPolicyEvidence([FromBody] DebugRecordPolicyEvidenceDto dto)
    {
        try
        {
            var engine = new PolicyEvidenceRecorderEngine(_policyEvidenceStore);
            var record = engine.RecordPolicyEvidence(
                dto.PolicyId, dto.ActorId, dto.Domain, dto.Operation, dto.Allowed, dto.Reason);
            return Ok(new
            {
                evidenceId = record.EvidenceId,
                policyId = record.PolicyId,
                actorId = record.ActorId,
                domain = record.Domain,
                operation = record.Operation,
                allowed = record.Allowed,
                reason = record.Reason,
                recordedAt = record.RecordedAt
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("policy/audit")]
    public IActionResult AuditPolicy([FromBody] DebugAuditPolicyDto dto)
    {
        var engine = new PolicyAuditEngine(_policyEvidenceStore);
        var query = new Whycespace.System.Upstream.WhycePolicy.Models.PolicyAuditQuery(
            dto.PolicyId, dto.ActorId, dto.Domain, dto.From, dto.To);
        var report = engine.AuditPolicy(query);
        return Ok(new
        {
            evidenceRecords = report.EvidenceRecords.Select(r => new
            {
                evidenceId = r.EvidenceId,
                policyId = r.PolicyId,
                actorId = r.ActorId,
                domain = r.Domain,
                operation = r.Operation,
                allowed = r.Allowed,
                reason = r.Reason,
                recordedAt = r.RecordedAt
            }),
            totalRecords = report.TotalRecords,
            generatedAt = report.GeneratedAt
        });
    }

    [HttpGet("audit")]
    public IActionResult GetAllAuditEvents()
    {
        var engine = new IdentityAuditEngine(_identityRegistry, _identityAuditStore);
        var events = engine.GetAllAuditEvents();
        return Ok(new
        {
            events = events.Select(e => new
            {
                eventId = e.EventId,
                identityId = e.IdentityId,
                eventType = e.EventType,
                description = e.Description,
                timestamp = e.Timestamp
            })
        });
    }

    // WhyceChain — Chain Ledger (Phase 2.0.40)

    [HttpGet("chain/ledger")]
    public IActionResult GetChainLedger()
    {
        var entries = _chainLedgerStore.GetAllEntries();
        return Ok(new { entries = entries.Select(e => new
        {
            e.EntryId,
            e.Timestamp,
            e.EventType,
            e.PayloadHash,
            e.PreviousHash,
            e.BlockId
        })});
    }

    [HttpGet("chain/ledger/{id}")]
    public IActionResult GetChainLedgerEntry(string id)
    {
        try
        {
            var entry = _chainLedgerStore.GetEntry(id);
            return Ok(new
            {
                entry.EntryId,
                entry.Timestamp,
                entry.EventType,
                entry.PayloadHash,
                entry.PreviousHash,
                entry.BlockId
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Ledger entry not found: {id}" });
        }
    }

    // WhyceChain — Chain Block (Phase 2.0.41)

    [HttpGet("chain/block/latest")]
    public IActionResult GetLatestChainBlock()
    {
        var block = _chainBlockStore.GetLatestBlock();
        if (block is null)
            return NotFound(new { error = "No blocks in chain" });

        return Ok(new
        {
            block.BlockId,
            block.BlockNumber,
            block.PreviousBlockHash,
            block.BlockHash,
            block.MerkleRoot,
            block.Timestamp,
            block.EntryIds
        });
    }

    [HttpGet("chain/block/{number:long}")]
    public IActionResult GetChainBlock(long number)
    {
        try
        {
            var block = _chainBlockStore.GetBlock(number);
            return Ok(new
            {
                block.BlockId,
                block.BlockNumber,
                block.PreviousBlockHash,
                block.BlockHash,
                block.MerkleRoot,
                block.Timestamp,
                block.EntryIds
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Block not found: {number}" });
        }
    }

    // WhyceChain — Immutable Event Ledger (Phase 2.0.42)

    [HttpGet("chain/events")]
    public IActionResult GetChainEvents()
    {
        var events = _chainEventStore.GetAllEvents();
        return Ok(new { events = events.Select(e => new
        {
            e.EventId,
            e.Domain,
            e.EventType,
            e.PayloadHash,
            e.Timestamp
        })});
    }

    [HttpGet("chain/events/{domain}")]
    public IActionResult GetChainEventsByDomain(string domain)
    {
        var events = _chainEventStore.GetEventsByDomain(domain);
        return Ok(new { events = events.Select(e => new
        {
            e.EventId,
            e.Domain,
            e.EventType,
            e.PayloadHash,
            e.Timestamp
        })});
    }

    // Governance — Guardian Registry (Phase 2.0.54)

    [HttpGet("governance/guardians")]
    public IActionResult GetGuardians()
    {
        var guardians = _guardianRegistryStore.ListGuardians();
        return Ok(new
        {
            guardians = guardians.Select(g => new
            {
                g.GuardianId,
                g.IdentityId,
                g.Name,
                status = g.Status.ToString(),
                g.Roles,
                g.CreatedAt,
                g.ActivatedAt
            })
        });
    }

    [HttpGet("governance/guardians/{id}")]
    public IActionResult GetGuardian(string id)
    {
        var guardian = _guardianRegistryStore.GetGuardian(id);
        if (guardian is null)
            return NotFound(new { error = $"Guardian not found: {id}" });

        return Ok(new
        {
            guardian.GuardianId,
            guardian.IdentityId,
            guardian.Name,
            status = guardian.Status.ToString(),
            guardian.Roles,
            guardian.CreatedAt,
            guardian.ActivatedAt
        });
    }

    [HttpPost("governance/guardians/register")]
    public IActionResult RegisterGuardian([FromBody] DebugRegisterGuardianDto dto)
    {
        try
        {
            var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);
            var guardian = engine.RegisterGuardian(dto.GuardianId, dto.IdentityId, dto.Name, dto.Roles);
            return Ok(new
            {
                guardian.GuardianId,
                guardian.IdentityId,
                guardian.Name,
                status = guardian.Status.ToString(),
                guardian.Roles,
                guardian.CreatedAt,
                guardian.ActivatedAt
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("governance/guardians/{id}/activate")]
    public IActionResult ActivateGuardian(string id)
    {
        try
        {
            var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);
            var guardian = engine.ActivateGuardian(id);
            return Ok(new
            {
                guardian.GuardianId,
                guardian.IdentityId,
                guardian.Name,
                status = guardian.Status.ToString(),
                guardian.Roles,
                guardian.CreatedAt,
                guardian.ActivatedAt
            });
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Guardian not found: {id}" }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("governance/guardians/{id}/deactivate")]
    public IActionResult DeactivateGuardian(string id)
    {
        try
        {
            var engine = new GuardianRegistryEngine(_guardianRegistryStore, _identityRegistry);
            var guardian = engine.DeactivateGuardian(id);
            return Ok(new
            {
                guardian.GuardianId,
                guardian.IdentityId,
                guardian.Name,
                status = guardian.Status.ToString(),
                guardian.Roles,
                guardian.CreatedAt,
                guardian.ActivatedAt
            });
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Guardian not found: {id}" }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Governance — Role Engine (Phase 2.0.55)

    [HttpPost("governance/roles")]
    public IActionResult CreateGovernanceRole([FromBody] DebugCreateGovernanceRoleDto dto)
    {
        try
        {
            var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);
            var role = engine.CreateRole(dto.RoleId, dto.Name, dto.Description, dto.Permissions);
            return Ok(new { role.RoleId, role.Name, role.Description, role.Permissions });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("governance/roles")]
    public IActionResult GetGovernanceRoles()
    {
        var roles = _governanceRoleStore.ListRoles();
        return Ok(new
        {
            roles = roles.Select(r => new { r.RoleId, r.Name, r.Description, r.Permissions })
        });
    }

    [HttpPost("governance/guardians/{id}/roles/assign")]
    public IActionResult AssignGovernanceRoleToGuardian(string id, [FromBody] DebugAssignGovernanceRoleDto dto)
    {
        try
        {
            var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);
            engine.AssignRole(id, dto.RoleId);
            var roles = engine.GetGuardianRoles(id);
            return Ok(new
            {
                guardianId = id,
                roles = roles.Select(r => new { r.RoleId, r.Name, r.Description, r.Permissions })
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("governance/guardians/{id}/roles/revoke")]
    public IActionResult RevokeGovernanceRoleFromGuardian(string id, [FromBody] DebugAssignGovernanceRoleDto dto)
    {
        try
        {
            var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);
            engine.RevokeRole(id, dto.RoleId);
            var roles = engine.GetGuardianRoles(id);
            return Ok(new
            {
                guardianId = id,
                roles = roles.Select(r => new { r.RoleId, r.Name, r.Description, r.Permissions })
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("governance/guardians/{id}/roles")]
    public IActionResult GetGuardianGovernanceRoles(string id)
    {
        var engine = new GovernanceRoleEngine(_governanceRoleStore, _guardianRegistryStore);
        var roles = engine.GetGuardianRoles(id);
        return Ok(new
        {
            guardianId = id,
            roles = roles.Select(r => new { r.RoleId, r.Name, r.Description, r.Permissions })
        });
    }

    [HttpGet("workflows/definitions")]
    public IActionResult GetWorkflowDefinitions()
    {
        var engine = new WorkflowDefinitionEngine(_workflowDefinitionStore);
        var workflows = engine.ListWorkflowDefinitions();
        return Ok(new
        {
            workflows = workflows.Select(w => new
            {
                workflowId = w.WorkflowId,
                name = w.Name,
                description = w.Description,
                version = w.Version,
                steps = w.Steps.Select(s => new { s.StepId, s.Name, s.EngineName, s.NextSteps }),
                createdAt = w.CreatedAt
            })
        });
    }

    [HttpGet("workflows/definitions/{id}")]
    public IActionResult GetWorkflowDefinition(string id)
    {
        try
        {
            var engine = new WorkflowDefinitionEngine(_workflowDefinitionStore);
            var workflow = engine.GetWorkflowDefinition(id);
            return Ok(new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                description = workflow.Description,
                version = workflow.Version,
                steps = workflow.Steps.Select(s => new { s.StepId, s.Name, s.EngineName, s.NextSteps }),
                createdAt = workflow.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow definition not found: '{id}'" });
        }
    }

    [HttpPost("workflows/definitions/register")]
    public IActionResult RegisterWorkflowDefinition([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        try
        {
            var engine = new WorkflowDefinitionEngine(_workflowDefinitionStore);
            var steps = dto.Steps.Select(s => new Whycespace.Contracts.Workflows.WorkflowStep(
                s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();
            var workflow = engine.RegisterWorkflowDefinition(dto.WorkflowId, dto.Name, dto.Description, dto.Version, steps);
            return Ok(new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                description = workflow.Description,
                version = workflow.Version,
                steps = workflow.Steps.Select(s => new { s.StepId, s.Name, s.EngineName, s.NextSteps }),
                createdAt = workflow.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("workflows/graph/{workflowId}")]
    public IActionResult GetWorkflowGraph(string workflowId)
    {
        try
        {
            var defEngine = new WorkflowDefinitionEngine(_workflowDefinitionStore);
            var workflow = defEngine.GetWorkflowDefinition(workflowId);
            var graphEngine = new Whycespace.Engines.T1M.WSS.Graph.WorkflowGraphEngine();
            var stepDefs = workflow.Steps.Select(s => new Whycespace.System.Midstream.WSS.Models.WorkflowStepDefinition(
                s.StepId, s.Name, s.EngineName, "", s.NextSteps, null)).ToList();
            var graph = graphEngine.BuildGraph(stepDefs);
            return Ok(new
            {
                workflowId = workflowId,
                transitions = graph.Transitions,
                startSteps = graphEngine.GetStartSteps(graph)
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow definition not found: '{workflowId}'" });
        }
    }

    [HttpPost("workflows/graph/validate")]
    public IActionResult ValidateWorkflowGraph([FromBody] DebugValidateGraphDto dto)
    {
        var graphEngine = new Whycespace.Engines.T1M.WSS.Graph.WorkflowGraphEngine();
        var transitions = dto.Transitions.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value);
        var graph = new Whycespace.System.Midstream.WSS.Models.WorkflowGraph(dto.WorkflowId, transitions);
        var violations = graphEngine.ValidateGraph(graph);
        return Ok(new
        {
            workflowId = dto.WorkflowId,
            isValid = violations.Count == 0,
            violations = violations
        });
    }

    [HttpGet("wss/templates")]
    public IActionResult GetWssTemplates()
    {
        var graphEngine = new WorkflowGraphEngine();
        var engine = new WorkflowTemplateEngine(_workflowTemplateStore, graphEngine);
        var templates = engine.ListTemplates();
        return Ok(new
        {
            templates = templates.Select(t => new
            {
                templateId = t.TemplateId,
                name = t.Name,
                version = t.Version,
                description = t.Description,
                stepCount = t.Steps.Count
            })
        });
    }

    [HttpGet("wss/templates/{id}")]
    public IActionResult GetWssTemplate(string id)
    {
        try
        {
            var graphEngine = new WorkflowGraphEngine();
            var engine = new WorkflowTemplateEngine(_workflowTemplateStore, graphEngine);
            var template = engine.GetTemplate(id);
            return Ok(new
            {
                templateId = template.TemplateId,
                name = template.Name,
                version = template.Version,
                description = template.Description,
                steps = template.Steps.Select(s => new
                {
                    stepId = s.StepId,
                    description = s.Description,
                    engine = s.Engine,
                    command = s.Command
                }),
                graph = template.Graph.Transitions
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow template not found: '{id}'" });
        }
    }

    [HttpPost("wss/templates/register")]
    public IActionResult RegisterWssTemplate([FromBody] DebugRegisterWorkflowTemplateDto dto)
    {
        try
        {
            var graphEngine = new WorkflowGraphEngine();
            var engine = new WorkflowTemplateEngine(_workflowTemplateStore, graphEngine);

            var steps = dto.Steps.Select(s => new Whycespace.System.Midstream.WSS.Models.WorkflowTemplateStep(
                s.StepId, s.Description, s.Engine, s.Command,
                s.Parameters ?? new Dictionary<string, string>(),
                null)).ToList();

            var transitions = dto.Transitions.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());

            var graph = new Whycespace.System.Midstream.WSS.Models.WorkflowGraph(
                dto.TemplateId, transitions);

            var template = new Whycespace.System.Midstream.WSS.Models.WorkflowTemplate(
                dto.TemplateId, dto.Name, dto.Version, dto.Description, steps, graph);

            engine.RegisterTemplate(template);

            return Ok(new
            {
                templateId = template.TemplateId,
                name = template.Name,
                version = template.Version,
                stepCount = template.Steps.Count
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("wss/templates/generate")]
    public IActionResult GenerateWssWorkflowFromTemplate([FromBody] DebugGenerateWorkflowFromTemplateDto dto)
    {
        try
        {
            var graphEngine = new WorkflowGraphEngine();
            var engine = new WorkflowTemplateEngine(_workflowTemplateStore, graphEngine);
            var definition = engine.GenerateWorkflowDefinition(dto.TemplateId, dto.Parameters);

            return Ok(new
            {
                workflowId = definition.WorkflowId,
                name = definition.Name,
                description = definition.Description,
                version = definition.Version,
                steps = definition.Steps.Select(s => new
                {
                    stepId = s.StepId,
                    name = s.Name,
                    engineName = s.EngineName,
                    nextSteps = s.NextSteps
                }),
                createdAt = definition.CreatedAt
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("wss/registry")]
    public IActionResult GetWssRegistry()
    {
        var workflows = _workflowRegistry.ListWorkflows();
        return Ok(new
        {
            workflows = workflows.Select(w => new
            {
                workflowId = w.WorkflowId,
                name = w.Name,
                version = w.Version,
                description = w.Description,
                stepCount = w.Steps.Count,
                createdAt = w.CreatedAt
            })
        });
    }

    [HttpGet("wss/registry/{id}")]
    public IActionResult GetWssRegistryEntry(string id)
    {
        try
        {
            var workflow = _workflowRegistry.GetWorkflow(id);
            return Ok(new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                version = workflow.Version,
                description = workflow.Description,
                steps = workflow.Steps.Select(s => new
                {
                    stepId = s.StepId,
                    name = s.Name,
                    engineName = s.EngineName,
                    nextSteps = s.NextSteps
                }),
                createdAt = workflow.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow not registered: '{id}'" });
        }
    }

    [HttpPost("wss/registry/register")]
    public IActionResult RegisterWssWorkflow([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        try
        {
            var steps = dto.Steps.Select(s =>
                new Whycespace.Contracts.Workflows.WorkflowStep(
                    s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();

            var definition = new Whycespace.System.Midstream.WSS.Models.WorkflowDefinition(
                dto.WorkflowId, dto.Name, dto.Description, dto.Version,
                steps, DateTimeOffset.UtcNow);

            _workflowRegistry.RegisterWorkflow(definition);

            return Ok(new
            {
                workflowId = definition.WorkflowId,
                name = definition.Name,
                version = definition.Version,
                stepCount = definition.Steps.Count,
                createdAt = definition.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("wss/registry/{id}")]
    public IActionResult RemoveWssWorkflow(string id)
    {
        try
        {
            _workflowRegistry.RemoveWorkflow(id);
            return Ok(new { message = $"Workflow removed: {id}" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow not found: '{id}'" });
        }
    }

    [HttpGet("wss/versions/{workflowId}")]
    public IActionResult GetWssVersions(string workflowId)
    {
        var engine = new WorkflowVersioningEngine(_workflowVersionStore, _workflowDefinitionStore);
        var versions = engine.ListWorkflowVersions(workflowId);
        return Ok(new
        {
            workflowId,
            versions = versions.Select(v => new
            {
                version = v.Version,
                name = v.Name,
                description = v.Description,
                createdAt = v.CreatedAt
            })
        });
    }

    [HttpGet("wss/versions/{workflowId}/{version}")]
    public IActionResult GetWssVersion(string workflowId, string version)
    {
        try
        {
            var engine = new WorkflowVersioningEngine(_workflowVersionStore, _workflowDefinitionStore);
            var workflow = engine.GetWorkflowVersion(workflowId, version);
            return Ok(new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                version = workflow.Version,
                description = workflow.Description,
                steps = workflow.Steps.Select(s => new { s.StepId, s.Name, s.EngineName, s.NextSteps }),
                createdAt = workflow.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Version '{version}' not found for workflow: '{workflowId}'" });
        }
    }

    [HttpGet("wss/versions/{workflowId}/latest")]
    public IActionResult GetWssLatestVersion(string workflowId)
    {
        try
        {
            var engine = new WorkflowVersioningEngine(_workflowVersionStore, _workflowDefinitionStore);
            var workflow = engine.GetLatestWorkflow(workflowId);
            return Ok(new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                version = workflow.Version,
                description = workflow.Description,
                createdAt = workflow.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"No versions found for workflow: '{workflowId}'" });
        }
    }

    [HttpPost("wss/versions/register")]
    public IActionResult RegisterWssVersion([FromBody] DebugRegisterWorkflowVersionDto dto)
    {
        try
        {
            var engine = new WorkflowVersioningEngine(_workflowVersionStore, _workflowDefinitionStore);
            var steps = dto.Steps.Select(s =>
                new Whycespace.Contracts.Workflows.WorkflowStep(s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();

            var workflow = new Whycespace.System.Midstream.WSS.Models.WorkflowDefinition(
                dto.WorkflowId, dto.Name, dto.Description, dto.Version, steps, DateTimeOffset.UtcNow);

            var result = engine.RegisterWorkflowVersion(workflow);
            return Ok(new
            {
                workflowId = result.WorkflowId,
                name = result.Name,
                version = result.Version,
                createdAt = result.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private WorkflowValidationOrchestrator CreateValidationOrchestrator()
    {
        var graphEngine = new WorkflowGraphEngine();
        var definitionEngine = new WorkflowDefinitionEngine(_workflowDefinitionStore);
        var templateEngine = new WorkflowTemplateEngine(_workflowTemplateStore, graphEngine);
        var versioningEngine = new WorkflowVersioningEngine(_workflowVersionStore, _workflowDefinitionStore);
        return new WorkflowValidationOrchestrator(definitionEngine, graphEngine, templateEngine, versioningEngine);
    }

    private static object FormatValidationResult(WorkflowValidationResult result) => new
    {
        isValid = result.IsValid,
        errors = result.Errors.Select(e => new { code = e.Code, message = e.Message, component = e.Component, stepId = e.StepId }),
        warnings = result.Warnings.Select(w => new { code = w.Code, message = w.Message, component = w.Component, stepId = w.StepId })
    };

    [HttpPost("wss/validation/workflow")]
    public IActionResult ValidateWssWorkflow([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var steps = dto.Steps.Select(s =>
            new Whycespace.Contracts.Workflows.WorkflowStep(s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();

        var workflow = new Whycespace.System.Midstream.WSS.Models.WorkflowDefinition(
            dto.WorkflowId, dto.Name, dto.Description, dto.Version, steps, DateTimeOffset.UtcNow);

        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateCompleteWorkflow(workflow);
        return Ok(FormatValidationResult(result));
    }

    [HttpPost("wss/validation/template")]
    public IActionResult ValidateWssTemplate([FromBody] DebugValidateWorkflowTemplateDto dto)
    {
        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateWorkflowTemplate(dto.TemplateId, dto.Parameters);
        return Ok(FormatValidationResult(result));
    }

    [HttpPost("wss/validation/version")]
    public IActionResult ValidateWssVersion([FromBody] DebugValidateWorkflowVersionDto dto)
    {
        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateWorkflowVersion(dto.WorkflowId, dto.Version);
        return Ok(FormatValidationResult(result));
    }

    private WorkflowDependencyAnalyzer CreateDependencyAnalyzer()
    {
        return new WorkflowDependencyAnalyzer(_workflowDefinitionStore);
    }

    private static object FormatDependencyResult(WorkflowDependencyResult result) => new
    {
        workflowId = result.WorkflowId,
        hasIssues = result.HasIssues,
        dependencies = result.Dependencies,
        executionOrder = result.ExecutionOrder,
        missingDependencies = result.MissingDependencies,
        circularDependencies = result.CircularDependencies,
        externalWorkflowDependencies = result.ExternalWorkflowDependencies
    };

    [HttpPost("wss/dependency/analyze")]
    public IActionResult AnalyzeWssDependencies([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var steps = dto.Steps.Select(s =>
            new Whycespace.Contracts.Workflows.WorkflowStep(s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();

        var workflow = new Whycespace.System.Midstream.WSS.Models.WorkflowDefinition(
            dto.WorkflowId, dto.Name, dto.Description, dto.Version, steps, DateTimeOffset.UtcNow);

        var analyzer = CreateDependencyAnalyzer();
        var result = analyzer.AnalyzeWorkflowDependencies(workflow);
        return Ok(FormatDependencyResult(result));
    }

    [HttpGet("wss/dependency/{workflowId}")]
    public IActionResult GetWssDependencies(string workflowId)
    {
        try
        {
            var workflow = _workflowDefinitionStore.Get(workflowId);
            var analyzer = CreateDependencyAnalyzer();
            var result = analyzer.AnalyzeWorkflowDependencies(workflow);
            return Ok(FormatDependencyResult(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow not found: '{workflowId}'" });
        }
    }

    [HttpPost("wss/dependency/resolve")]
    public IActionResult ResolveWssDependencies([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var steps = dto.Steps.Select(s =>
            new Whycespace.Contracts.Workflows.WorkflowStep(s.StepId, s.Name, s.EngineName, s.NextSteps)).ToList();

        var workflow = new Whycespace.System.Midstream.WSS.Models.WorkflowDefinition(
            dto.WorkflowId, dto.Name, dto.Description, dto.Version, steps, DateTimeOffset.UtcNow);

        var analyzer = CreateDependencyAnalyzer();
        var order = analyzer.ResolveExecutionOrder(workflow);
        return Ok(new
        {
            workflowId = dto.WorkflowId,
            executionOrder = order
        });
    }

    // --- WSS Engine Mapping (Phase 2.1.8) ---

    [HttpGet("wss/engines")]
    public IActionResult GetWssEngines()
    {
        var mapper = new WorkflowStepEngineMapper(_engineMappingStore);
        var engines = mapper.ListEngines();
        return Ok(new { engines });
    }

    [HttpPost("wss/engines/register")]
    public IActionResult RegisterWssEngine([FromBody] DebugRegisterEngineMappingDto dto)
    {
        var mapper = new WorkflowStepEngineMapper(_engineMappingStore);
        mapper.RegisterEngine(dto.EngineName, dto.RuntimeIdentifier);
        return Ok(new { message = $"Engine '{dto.EngineName}' mapped to '{dto.RuntimeIdentifier}'" });
    }

    [HttpGet("wss/engines/{engineName}")]
    public IActionResult ResolveWssEngine(string engineName)
    {
        var mapper = new WorkflowStepEngineMapper(_engineMappingStore);
        if (!mapper.EngineExists(engineName))
            return NotFound(new { message = $"Engine mapping not found: '{engineName}'" });

        var runtimeIdentifier = mapper.ResolveEngine(engineName);
        return Ok(new { engineName, runtimeIdentifier });
    }

    // --- WSS Instance Registry (Phase 2.1.9) ---

    [HttpGet("wss/instances")]
    public IActionResult GetWssInstances()
    {
        var registry = new Whycespace.Engines.T1M.WSS.Instance.WorkflowInstanceRegistry(_instanceRegistryStore);
        var instances = registry.ListInstances();
        return Ok(new { count = instances.Count, instances });
    }

    [HttpGet("wss/instances/{instanceId}")]
    public IActionResult GetWssInstance(string instanceId)
    {
        var registry = new Whycespace.Engines.T1M.WSS.Instance.WorkflowInstanceRegistry(_instanceRegistryStore);
        try
        {
            var instance = registry.GetInstance(instanceId);
            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Instance not found: '{instanceId}'" });
        }
    }

    [HttpPost("wss/instances/create")]
    public IActionResult CreateWssInstance([FromBody] DebugCreateWssInstanceDto dto)
    {
        var registry = new Whycespace.Engines.T1M.WSS.Instance.WorkflowInstanceRegistry(_instanceRegistryStore);
        var instance = registry.CreateInstance(dto.WorkflowId, dto.Version, dto.Context);
        return Ok(instance);
    }

    [HttpPost("wss/instances/update")]
    public IActionResult UpdateWssInstance([FromBody] DebugUpdateWssInstanceDto dto)
    {
        var registry = new Whycespace.Engines.T1M.WSS.Instance.WorkflowInstanceRegistry(_instanceRegistryStore);
        try
        {
            var updated = registry.UpdateInstanceState(dto.InstanceId, dto.CurrentStep, dto.Status);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Instance not found: '{dto.InstanceId}'" });
        }
    }

    [HttpDelete("wss/instances/{instanceId}")]
    public IActionResult RemoveWssInstance(string instanceId)
    {
        var registry = new Whycespace.Engines.T1M.WSS.Instance.WorkflowInstanceRegistry(_instanceRegistryStore);
        try
        {
            registry.RemoveInstance(instanceId);
            return Ok(new { message = $"Instance '{instanceId}' removed" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Instance not found: '{instanceId}'" });
        }
    }

    // --- WSS Workflow State Store (Phase 2.1.10) ---

    [HttpGet("wss/state")]
    public IActionResult GetWssActiveStates()
    {
        var states = _wssWorkflowStateStore.ListActiveStates();
        return Ok(new { count = states.Count, states });
    }

    [HttpGet("wss/state/{instanceId}")]
    public IActionResult GetWssState(string instanceId)
    {
        try
        {
            var state = _wssWorkflowStateStore.GetState(instanceId);
            return Ok(state);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow state not found: '{instanceId}'" });
        }
    }

    [HttpPost("wss/state/save")]
    public IActionResult SaveWssState([FromBody] DebugSaveWssStateDto dto)
    {
        var state = new WorkflowState(
            dto.InstanceId,
            dto.WorkflowId,
            dto.WorkflowVersion,
            string.Empty,
            new List<string>(),
            WorkflowInstanceStatus.Created,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            dto.ExecutionContext ?? new Dictionary<string, object>()
        );

        _wssWorkflowStateStore.SaveState(state);
        return Ok(state);
    }

    [HttpPost("wss/state/update")]
    public IActionResult UpdateWssState([FromBody] DebugUpdateWssStateDto dto)
    {
        try
        {
            var updated = _wssWorkflowStateStore.UpdateState(dto.InstanceId, dto.CurrentStep, dto.Status);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow state not found: '{dto.InstanceId}'" });
        }
    }

    [HttpDelete("wss/state/{instanceId}")]
    public IActionResult DeleteWssState(string instanceId)
    {
        try
        {
            _wssWorkflowStateStore.DeleteState(instanceId);
            return Ok(new { message = $"Workflow state '{instanceId}' deleted" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Workflow state not found: '{instanceId}'" });
        }
    }
}

public sealed record DebugRunWorkflowDto(string WorkflowName, Dictionary<string, object>? Context);
public sealed record DebugReplayEventDto(string EventType, Guid AggregateId, Dictionary<string, object>? Payload);
public sealed record DebugRunSimulationDto(Guid ScenarioId);
public sealed record DebugGenerateClusterDto(string TemplateName);
public sealed record DebugAuthenticateDto(Guid IdentityId, Guid DeviceId);
public sealed record DebugAuthorizeDto(Guid IdentityId, string Resource, string Action, string Scope);
public sealed record DebugRevokeSessionDto(Guid SessionId);
public sealed record DebugGrantConsentDto(string Target, string Scope);
public sealed record DebugRevokeConsentDto(Guid ConsentId);
public sealed record DebugCreateRelationshipDto(Guid TargetEntityId, string Relationship);
public sealed record DebugRemoveRelationshipDto(Guid EdgeId);
public sealed record DebugRegisterServiceDto(string Name, string Type, string Secret);
public sealed record DebugRevokeServiceDto(Guid ServiceId);
public sealed record DebugRegisterFederationDto(string Provider, string ExternalIdentityId, Guid InternalIdentityId);
public sealed record DebugRevokeFederationDto(Guid FederationId);
public sealed record DebugCreateRecoveryDto(Guid IdentityId, string Reason);
public sealed record DebugRecoveryActionDto(Guid RecoveryId);
public sealed record DebugRevokeIdentityDto(Guid IdentityId, string Reason);
public sealed record DebugEvaluatePolicyDto(Guid IdentityId);
public sealed record DebugRecordAuditDto(Guid IdentityId, string EventType, string Description);
public sealed record DebugParsePolicyDto(string Dsl);
public sealed record DebugEvaluatePoliciesDto(Guid ActorId, string Domain, Dictionary<string, string> Attributes);
public sealed record DebugSimulatePolicyDto(string Domain, string ActorId, Dictionary<string, string> Attributes);
public sealed record DebugForecastPolicyDto(string Domain, List<DebugSimulatePolicyDto> SimulationContexts);
public sealed record DebugLifecycleTransitionDto(string PolicyId, string Version);
public sealed record DebugSetRolloutDto(string PolicyId, string Version, string Strategy, int Percentage, List<string>? Actors, List<string>? Domains);
public sealed record DebugCheckRolloutDto(string PolicyId, string Version, string ActorId, string Domain);
public sealed record DebugGovernanceAssignDto(string ActorId, string Role);
public sealed record DebugConstitutionalRegisterDto(string PolicyId, string Version, string ProtectionLevel);
public sealed record DebugConstitutionalCheckDto(string PolicyId, string Version);
public sealed record DebugBuildPolicyContextDto(Guid ActorId, string TargetDomain, Dictionary<string, string> Attributes);
public sealed record DebugEnforcePolicyDto(string ActorId, string Domain, string Operation, Dictionary<string, string> Attributes);
public sealed record DebugBindPolicyDomainDto(string PolicyId, string Version, string Domain);
public sealed record DebugRecordPolicyEvidenceDto(string PolicyId, string ActorId, string Domain, string Operation, bool Allowed, string Reason);
public sealed record DebugAuditPolicyDto(string? PolicyId, string? ActorId, string? Domain, DateTime? From, DateTime? To);
public sealed record DebugRegisterGuardianDto(string GuardianId, Guid IdentityId, string Name, List<string> Roles);
public sealed record DebugCreateGovernanceRoleDto(string RoleId, string Name, string Description, List<string> Permissions);
public sealed record DebugAssignGovernanceRoleDto(string RoleId);
public sealed record DebugRegisterWorkflowDefinitionDto(string WorkflowId, string Name, string Description, string Version, List<DebugWorkflowStepDto> Steps);
public sealed record DebugWorkflowStepDto(string StepId, string Name, string EngineName, List<string> NextSteps);
public sealed record DebugRegisterWorkflowTemplateDto(string TemplateId, string Name, int Version, string Description, List<DebugWorkflowTemplateStepDto> Steps, Dictionary<string, List<string>> Transitions);
public sealed record DebugWorkflowTemplateStepDto(string StepId, string Description, string Engine, string Command, Dictionary<string, string>? Parameters);
public sealed record DebugGenerateWorkflowFromTemplateDto(string TemplateId, Dictionary<string, string> Parameters);
public sealed record DebugRegisterWssWorkflowDto(string WorkflowId);
public sealed record DebugRegisterWorkflowVersionDto(string WorkflowId, string Name, string Description, string Version, List<DebugWorkflowStepDto> Steps);
public sealed record DebugValidateGraphDto(string WorkflowId, Dictionary<string, List<string>> Transitions);
public sealed record DebugValidateWorkflowTemplateDto(string TemplateId, Dictionary<string, string> Parameters);
public sealed record DebugValidateWorkflowVersionDto(string WorkflowId, string Version);
public sealed record DebugRegisterEngineMappingDto(string EngineName, string RuntimeIdentifier);
public sealed record DebugCreateWssInstanceDto(string WorkflowId, string Version, Dictionary<string, object>? Context);
public sealed record DebugUpdateWssInstanceDto(string InstanceId, string CurrentStep, WorkflowInstanceStatus Status);
public sealed record DebugSaveWssStateDto(string InstanceId, string WorkflowId, string WorkflowVersion, Dictionary<string, object>? ExecutionContext);
public sealed record DebugUpdateWssStateDto(string InstanceId, string CurrentStep, WorkflowInstanceStatus Status);
