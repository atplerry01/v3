namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.ArchitectureGuardrails.Enforcement;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Events;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.ClusterDomain;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Services;
using Whycespace.ClusterTemplatePlatform;
using Whycespace.EconomicDomain;
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
using Whycespace.System.Upstream.WhycePolicy.Stores;

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
        PolicyEvidenceStore policyEvidenceStore)
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
        var engineAssembly = typeof(Whycespace.Engines.T2E_Execution.RideExecutionEngine).Assembly;
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
