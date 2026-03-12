namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.ArchitectureGuardrails.Enforcement;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Events;
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
using Whycespace.System.Upstream.WhycePolicy.Stores;
using Whycespace.System.Upstream.WhyceChain.Stores;
using Whycespace.System.Upstream.Governance.Stores;

[ApiController]
[Route("dev")]
public sealed class DebugController : ControllerBase
{
    private readonly IPlatformDispatcher _dispatcher;
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
    private readonly IdentityDeviceStore _identityDeviceStore;
    private readonly IdentitySessionStore _identitySessionStore;
    private readonly IdentityConsentStore _identityConsentStore;
    private readonly IdentityGraphStore _identityGraphStore;
    private readonly IdentityServiceStore _identityServiceStore;
    private readonly IdentityFederationStore _identityFederationStore;
    private readonly PolicyRolloutStore _policyRolloutStore;
    private readonly PolicyDecisionCacheStore _policyDecisionCacheStore;
    private readonly ConstitutionalPolicyStore _constitutionalPolicyStore;
    private readonly ChainLedgerStore _chainLedgerStore;
    private readonly ChainBlockStore _chainBlockStore;
    private readonly ChainEventStore _chainEventStore;
    private readonly GuardianRegistryStore _guardianRegistryStore;
    private readonly GovernanceRoleStore _governanceRoleStore;

    public DebugController(
        IPlatformDispatcher dispatcher,
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
        IdentityDeviceStore identityDeviceStore,
        IdentitySessionStore identitySessionStore,
        IdentityConsentStore identityConsentStore,
        IdentityGraphStore identityGraphStore,
        IdentityServiceStore identityServiceStore,
        IdentityFederationStore identityFederationStore,
        PolicyRolloutStore policyRolloutStore,
        PolicyDecisionCacheStore policyDecisionCacheStore,
        ConstitutionalPolicyStore constitutionalPolicyStore,
        ChainLedgerStore chainLedgerStore,
        ChainBlockStore chainBlockStore,
        ChainEventStore chainEventStore,
        GuardianRegistryStore guardianRegistryStore,
        GovernanceRoleStore governanceRoleStore)
    {
        _dispatcher = dispatcher;
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
        _identityDeviceStore = identityDeviceStore;
        _identitySessionStore = identitySessionStore;
        _identityConsentStore = identityConsentStore;
        _identityGraphStore = identityGraphStore;
        _identityServiceStore = identityServiceStore;
        _identityFederationStore = identityFederationStore;
        _policyRolloutStore = policyRolloutStore;
        _policyDecisionCacheStore = policyDecisionCacheStore;
        _constitutionalPolicyStore = constitutionalPolicyStore;
        _chainLedgerStore = chainLedgerStore;
        _chainBlockStore = chainBlockStore;
        _chainEventStore = chainEventStore;
        _guardianRegistryStore = guardianRegistryStore;
        _governanceRoleStore = governanceRoleStore;
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
        var sharedAssembly = typeof(Whycespace.Contracts.Engines.IEngine).Assembly;

        var report = enforcement.Validate(sharedAssembly, sharedAssembly);

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
    public async Task<IActionResult> CalculateTrustScore(Guid id)
    {
        var result = await _dispatcher.DispatchAsync("identity.trustscore.calculate", new Dictionary<string, object>
        {
            ["identityId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> Authenticate([FromBody] DebugAuthenticateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.authenticate", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId,
            ["deviceId"] = dto.DeviceId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromBody] DebugAuthorizeDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.authorize", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId,
            ["resource"] = dto.Resource,
            ["action"] = dto.Action,
            ["scope"] = dto.Scope
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> GrantConsent(Guid id, [FromBody] DebugGrantConsentDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.consent.grant", new Dictionary<string, object>
        {
            ["identityId"] = id,
            ["target"] = dto.Target,
            ["scope"] = dto.Scope
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> GetIdentityRelationships(Guid id)
    {
        var result = await _dispatcher.DispatchAsync("identity.graph.getRelationships", new Dictionary<string, object>
        {
            ["identityId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("identity/{id:guid}/relationship")]
    public async Task<IActionResult> CreateRelationship(Guid id, [FromBody] DebugCreateRelationshipDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.graph.createRelationship", new Dictionary<string, object>
        {
            ["identityId"] = id,
            ["targetEntityId"] = dto.TargetEntityId,
            ["relationship"] = dto.Relationship
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("relationship/remove")]
    public IActionResult RemoveRelationship([FromBody] DebugRemoveRelationshipDto dto)
    {
        _identityGraphStore.Remove(dto.EdgeId);
        return Ok(new { message = "Relationship removed", edgeId = dto.EdgeId });
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var result = await _dispatcher.DispatchAsync("identity.service.getServices", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("services/register")]
    public async Task<IActionResult> RegisterService([FromBody] DebugRegisterServiceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.service.register", new Dictionary<string, object>
        {
            ["name"] = dto.Name,
            ["type"] = dto.Type,
            ["secret"] = dto.Secret
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("services/revoke")]
    public IActionResult RevokeService([FromBody] DebugRevokeServiceDto dto)
    {
        _identityServiceStore.Revoke(dto.ServiceId);
        return Ok(new { message = "Service revoked", serviceId = dto.ServiceId });
    }

    [HttpGet("federations")]
    public async Task<IActionResult> GetFederations()
    {
        var result = await _dispatcher.DispatchAsync("identity.federation.getFederations", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("federation/register")]
    public async Task<IActionResult> RegisterFederation([FromBody] DebugRegisterFederationDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.federation.register", new Dictionary<string, object>
        {
            ["provider"] = dto.Provider,
            ["externalIdentityId"] = dto.ExternalIdentityId,
            ["internalIdentityId"] = dto.InternalIdentityId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("federation/revoke")]
    public IActionResult RevokeFederation([FromBody] DebugRevokeFederationDto dto)
    {
        _identityFederationStore.Revoke(dto.FederationId);
        return Ok(new { message = "Federation revoked", federationId = dto.FederationId });
    }

    [HttpGet("identity/{id:guid}/recoveries")]
    public async Task<IActionResult> GetIdentityRecoveries(Guid id)
    {
        var result = await _dispatcher.DispatchAsync("identity.recovery.getRecoveries", new Dictionary<string, object>
        {
            ["identityId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("recovery/create")]
    public async Task<IActionResult> CreateRecovery([FromBody] DebugCreateRecoveryDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.recovery.create", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId,
            ["reason"] = dto.Reason
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("recovery/approve")]
    public async Task<IActionResult> ApproveRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.recovery.approve", new Dictionary<string, object>
        {
            ["recoveryId"] = dto.RecoveryId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("recovery/reject")]
    public async Task<IActionResult> RejectRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.recovery.reject", new Dictionary<string, object>
        {
            ["recoveryId"] = dto.RecoveryId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("recovery/complete")]
    public async Task<IActionResult> CompleteRecovery([FromBody] DebugRecoveryActionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.recovery.complete", new Dictionary<string, object>
        {
            ["recoveryId"] = dto.RecoveryId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("identity/{id:guid}/revocations")]
    public async Task<IActionResult> GetIdentityRevocations(Guid id)
    {
        var result = await _dispatcher.DispatchAsync("identity.revocation.getRevocations", new Dictionary<string, object>
        {
            ["identityId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("identity/revoke")]
    public async Task<IActionResult> RevokeIdentity([FromBody] DebugRevokeIdentityDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.revocation.revoke", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId,
            ["reason"] = dto.Reason
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("revocations")]
    public async Task<IActionResult> GetAllRevocations()
    {
        var result = await _dispatcher.DispatchAsync("identity.revocation.getAll", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("identity/policy/evaluate")]
    public async Task<IActionResult> EvaluateIdentityPolicy([FromBody] DebugEvaluatePolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.policy.evaluate", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("identity/{id:guid}/audit")]
    public async Task<IActionResult> GetIdentityAudit(Guid id)
    {
        var result = await _dispatcher.DispatchAsync("identity.audit.get", new Dictionary<string, object>
        {
            ["identityId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("identity/audit")]
    public async Task<IActionResult> RecordAuditEvent([FromBody] DebugRecordAuditDto dto)
    {
        var result = await _dispatcher.DispatchAsync("identity.audit.record", new Dictionary<string, object>
        {
            ["identityId"] = dto.IdentityId,
            ["eventType"] = dto.EventType,
            ["description"] = dto.Description
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/parse")]
    public async Task<IActionResult> ParsePolicyDsl([FromBody] DebugParsePolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.dsl.parse", new Dictionary<string, object>
        {
            ["dsl"] = dto.Dsl
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies()
    {
        var result = await _dispatcher.DispatchAsync("policy.registry.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policies/{id}")]
    public async Task<IActionResult> GetPolicy(string id)
    {
        var result = await _dispatcher.DispatchAsync("policy.registry.get", new Dictionary<string, object>
        {
            ["policyId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policies/{id}/versions")]
    public async Task<IActionResult> GetPolicyVersions(string id)
    {
        var result = await _dispatcher.DispatchAsync("policy.version.list", new Dictionary<string, object>
        {
            ["policyId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policies/{id}/dependencies")]
    public async Task<IActionResult> GetPolicyDependencies(string id)
    {
        var result = await _dispatcher.DispatchAsync("policy.dependency.get", new Dictionary<string, object>
        {
            ["policyId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/evaluate")]
    public async Task<IActionResult> EvaluatePolicies([FromBody] DebugEvaluatePoliciesDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.evaluate", new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId,
            ["domain"] = dto.Domain,
            ["attributes"] = dto.Attributes
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/context")]
    public async Task<IActionResult> BuildPolicyContext([FromBody] DebugBuildPolicyContextDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.context.build", new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId,
            ["targetDomain"] = dto.TargetDomain,
            ["attributes"] = dto.Attributes
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/cache")]
    public async Task<IActionResult> GetPolicyCache()
    {
        var result = await _dispatcher.DispatchAsync("policy.cache.get", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("policy/cache")]
    public IActionResult ClearPolicyCache()
    {
        _policyDecisionCacheStore.Clear();
        return Ok(new { message = "Policy decision cache cleared." });
    }

    [HttpPost("policy/simulate")]
    public async Task<IActionResult> SimulatePolicyEvaluation([FromBody] DebugSimulatePolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.simulate", new Dictionary<string, object>
        {
            ["domain"] = dto.Domain,
            ["actorId"] = dto.ActorId,
            ["attributes"] = dto.Attributes
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/conflicts/{domain}")]
    public async Task<IActionResult> DetectPolicyConflicts(string domain)
    {
        var result = await _dispatcher.DispatchAsync("policy.conflict.detect", new Dictionary<string, object>
        {
            ["domain"] = domain
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/forecast")]
    public async Task<IActionResult> ForecastPolicyImpact([FromBody] DebugForecastPolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.forecast", new Dictionary<string, object>
        {
            ["domain"] = dto.Domain,
            ["simulationContexts"] = dto.SimulationContexts
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/lifecycle/approve")]
    public async Task<IActionResult> ApprovePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.lifecycle.approve", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/lifecycle/activate")]
    public async Task<IActionResult> ActivatePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.lifecycle.activate", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/lifecycle/deprecate")]
    public async Task<IActionResult> DeprecatePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.lifecycle.deprecate", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/lifecycle/archive")]
    public async Task<IActionResult> ArchivePolicyLifecycle([FromBody] DebugLifecycleTransitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.lifecycle.archive", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/lifecycle/{policyId}/{version}")]
    public async Task<IActionResult> GetPolicyLifecycle(string policyId, string version)
    {
        var result = await _dispatcher.DispatchAsync("policy.lifecycle.get", new Dictionary<string, object>
        {
            ["policyId"] = policyId,
            ["version"] = version
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> CheckPolicyRollout([FromBody] DebugCheckRolloutDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.rollout.check", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version,
            ["actorId"] = dto.ActorId,
            ["domain"] = dto.Domain
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/governance/assign")]
    public async Task<IActionResult> AssignGovernanceAuthority([FromBody] DebugGovernanceAssignDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.governance.assign", new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId,
            ["role"] = dto.Role
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/governance/{actorId}")]
    public async Task<IActionResult> GetGovernanceAuthority(string actorId)
    {
        var result = await _dispatcher.DispatchAsync("policy.governance.get", new Dictionary<string, object>
        {
            ["actorId"] = actorId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/governance/check")]
    public async Task<IActionResult> CheckGovernanceAuthority([FromBody] DebugGovernanceAssignDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.governance.check", new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId,
            ["role"] = dto.Role
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/constitutional/register")]
    public async Task<IActionResult> RegisterConstitutionalPolicy([FromBody] DebugConstitutionalRegisterDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.constitutional.register", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version,
            ["protectionLevel"] = dto.ProtectionLevel
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> EnforcePolicy([FromBody] DebugEnforcePolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.enforce", new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId,
            ["domain"] = dto.Domain,
            ["operation"] = dto.Operation,
            ["attributes"] = dto.Attributes
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/domain/bind")]
    public async Task<IActionResult> BindPolicyToDomain([FromBody] DebugBindPolicyDomainDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.domain.bind", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["version"] = dto.Version,
            ["domain"] = dto.Domain
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/domain/{policyId}")]
    public async Task<IActionResult> GetDomainsForPolicy(string policyId)
    {
        var result = await _dispatcher.DispatchAsync("policy.domain.getDomains", new Dictionary<string, object>
        {
            ["policyId"] = policyId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/domain/policies/{domain}")]
    public async Task<IActionResult> GetPoliciesForDomain(string domain)
    {
        var result = await _dispatcher.DispatchAsync("policy.domain.getPolicies", new Dictionary<string, object>
        {
            ["domain"] = domain
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/monitoring")]
    public async Task<IActionResult> GetAllPolicyMonitoring()
    {
        var result = await _dispatcher.DispatchAsync("policy.monitoring.getAll", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/monitoring/{policyId}")]
    public async Task<IActionResult> GetPolicyMonitoring(string policyId)
    {
        var result = await _dispatcher.DispatchAsync("policy.monitoring.get", new Dictionary<string, object>
        {
            ["policyId"] = policyId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/evidence")]
    public async Task<IActionResult> GetAllPolicyEvidence()
    {
        var result = await _dispatcher.DispatchAsync("policy.evidence.getAll", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("policy/evidence/{evidenceId}")]
    public async Task<IActionResult> GetPolicyEvidence(string evidenceId)
    {
        var result = await _dispatcher.DispatchAsync("policy.evidence.get", new Dictionary<string, object>
        {
            ["evidenceId"] = evidenceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/evidence")]
    public async Task<IActionResult> RecordPolicyEvidence([FromBody] DebugRecordPolicyEvidenceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.evidence.record", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId,
            ["actorId"] = dto.ActorId,
            ["domain"] = dto.Domain,
            ["operation"] = dto.Operation,
            ["allowed"] = dto.Allowed,
            ["reason"] = dto.Reason
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("policy/audit")]
    public async Task<IActionResult> AuditPolicy([FromBody] DebugAuditPolicyDto dto)
    {
        var result = await _dispatcher.DispatchAsync("policy.audit", new Dictionary<string, object>
        {
            ["policyId"] = dto.PolicyId!,
            ["actorId"] = dto.ActorId!,
            ["domain"] = dto.Domain!,
            ["from"] = dto.From!,
            ["to"] = dto.To!
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAllAuditEvents()
    {
        var result = await _dispatcher.DispatchAsync("identity.audit.getAll", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> RegisterGuardian([FromBody] DebugRegisterGuardianDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.guardian.register", new Dictionary<string, object>
        {
            ["guardianId"] = dto.GuardianId,
            ["identityId"] = dto.IdentityId,
            ["name"] = dto.Name,
            ["roles"] = dto.Roles
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("governance/guardians/{id}/activate")]
    public async Task<IActionResult> ActivateGuardian(string id)
    {
        var result = await _dispatcher.DispatchAsync("governance.guardian.activate", new Dictionary<string, object>
        {
            ["guardianId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("governance/guardians/{id}/deactivate")]
    public async Task<IActionResult> DeactivateGuardian(string id)
    {
        var result = await _dispatcher.DispatchAsync("governance.guardian.deactivate", new Dictionary<string, object>
        {
            ["guardianId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    // Governance — Role Engine (Phase 2.0.55)

    [HttpPost("governance/roles")]
    public async Task<IActionResult> CreateGovernanceRole([FromBody] DebugCreateGovernanceRoleDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.role.create", new Dictionary<string, object>
        {
            ["roleId"] = dto.RoleId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["permissions"] = dto.Permissions
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
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
    public async Task<IActionResult> AssignGovernanceRoleToGuardian(string id, [FromBody] DebugAssignGovernanceRoleDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.role.assign", new Dictionary<string, object>
        {
            ["guardianId"] = id,
            ["roleId"] = dto.RoleId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("governance/guardians/{id}/roles/revoke")]
    public async Task<IActionResult> RevokeGovernanceRoleFromGuardian(string id, [FromBody] DebugAssignGovernanceRoleDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.role.revoke", new Dictionary<string, object>
        {
            ["guardianId"] = id,
            ["roleId"] = dto.RoleId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("governance/guardians/{id}/roles")]
    public async Task<IActionResult> GetGuardianGovernanceRoles(string id)
    {
        var result = await _dispatcher.DispatchAsync("governance.role.getGuardianRoles", new Dictionary<string, object>
        {
            ["guardianId"] = id
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("workflows/definitions")]
    public async Task<IActionResult> GetWorkflowDefinitions()
    {
        var result = await _dispatcher.DispatchAsync("wss.definition.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("workflows/definitions/{id}")]
    public async Task<IActionResult> GetWorkflowDefinition(string id)
    {
        var result = await _dispatcher.DispatchAsync("wss.definition.get", new Dictionary<string, object>
        {
            ["workflowId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("workflows/definitions/register")]
    public async Task<IActionResult> RegisterWorkflowDefinition([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.definition.register", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("workflows/graph/{workflowId}")]
    public async Task<IActionResult> GetWorkflowGraph(string workflowId)
    {
        var result = await _dispatcher.DispatchAsync("wss.graph.build", new Dictionary<string, object>
        {
            ["workflowId"] = workflowId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("workflows/graph/validate")]
    public async Task<IActionResult> ValidateWorkflowGraph([FromBody] DebugValidateGraphDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.graph.validate", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["transitions"] = dto.Transitions
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/templates")]
    public async Task<IActionResult> GetWssTemplates()
    {
        var result = await _dispatcher.DispatchAsync("wss.template.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/templates/{id}")]
    public async Task<IActionResult> GetWssTemplate(string id)
    {
        var result = await _dispatcher.DispatchAsync("wss.template.get", new Dictionary<string, object>
        {
            ["templateId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/templates/register")]
    public async Task<IActionResult> RegisterWssTemplate([FromBody] DebugRegisterWorkflowTemplateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.template.register", new Dictionary<string, object>
        {
            ["templateId"] = dto.TemplateId,
            ["name"] = dto.Name,
            ["version"] = dto.Version,
            ["description"] = dto.Description,
            ["steps"] = dto.Steps,
            ["transitions"] = dto.Transitions
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/templates/generate")]
    public async Task<IActionResult> GenerateWssWorkflowFromTemplate([FromBody] DebugGenerateWorkflowFromTemplateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.template.generate", new Dictionary<string, object>
        {
            ["templateId"] = dto.TemplateId,
            ["parameters"] = dto.Parameters
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/registry")]
    public async Task<IActionResult> GetWssRegistry()
    {
        var result = await _dispatcher.DispatchAsync("wss.registry.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/registry/{id}")]
    public async Task<IActionResult> GetWssRegistryEntry(string id)
    {
        var result = await _dispatcher.DispatchAsync("wss.registry.get", new Dictionary<string, object>
        {
            ["workflowId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/registry/register")]
    public async Task<IActionResult> RegisterWssWorkflow([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.registry.register", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("wss/registry/{id}")]
    public async Task<IActionResult> RemoveWssWorkflow(string id)
    {
        var result = await _dispatcher.DispatchAsync("wss.registry.remove", new Dictionary<string, object>
        {
            ["workflowId"] = id
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/versions/{workflowId}")]
    public async Task<IActionResult> GetWssVersions(string workflowId)
    {
        var result = await _dispatcher.DispatchAsync("wss.version.list", new Dictionary<string, object>
        {
            ["workflowId"] = workflowId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/versions/{workflowId}/{version}")]
    public async Task<IActionResult> GetWssVersion(string workflowId, string version)
    {
        var result = await _dispatcher.DispatchAsync("wss.version.get", new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["version"] = version
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/versions/{workflowId}/latest")]
    public async Task<IActionResult> GetWssLatestVersion(string workflowId)
    {
        var result = await _dispatcher.DispatchAsync("wss.version.latest", new Dictionary<string, object>
        {
            ["workflowId"] = workflowId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/versions/register")]
    public async Task<IActionResult> RegisterWssVersion([FromBody] DebugRegisterWorkflowVersionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.version.register", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/validation/workflow")]
    public async Task<IActionResult> ValidateWssWorkflow([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.validation.workflow", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/validation/template")]
    public async Task<IActionResult> ValidateWssTemplate([FromBody] DebugValidateWorkflowTemplateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.validation.template", new Dictionary<string, object>
        {
            ["templateId"] = dto.TemplateId,
            ["parameters"] = dto.Parameters
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/validation/version")]
    public async Task<IActionResult> ValidateWssVersion([FromBody] DebugValidateWorkflowVersionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.validation.version", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["version"] = dto.Version
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/dependency/analyze")]
    public async Task<IActionResult> AnalyzeWssDependencies([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.dependency.analyze", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/dependency/{workflowId}")]
    public async Task<IActionResult> GetWssDependencies(string workflowId)
    {
        var result = await _dispatcher.DispatchAsync("wss.dependency.get", new Dictionary<string, object>
        {
            ["workflowId"] = workflowId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/dependency/resolve")]
    public async Task<IActionResult> ResolveWssDependencies([FromBody] DebugRegisterWorkflowDefinitionDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.dependency.resolve", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["name"] = dto.Name,
            ["description"] = dto.Description,
            ["version"] = dto.Version,
            ["steps"] = dto.Steps
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Engine Mapping (Phase 2.1.8) ---

    [HttpGet("wss/engines")]
    public async Task<IActionResult> GetWssEngines()
    {
        var result = await _dispatcher.DispatchAsync("wss.engine.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/engines/register")]
    public async Task<IActionResult> RegisterWssEngine([FromBody] DebugRegisterEngineMappingDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.engine.register", new Dictionary<string, object>
        {
            ["engineName"] = dto.EngineName,
            ["runtimeIdentifier"] = dto.RuntimeIdentifier
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/engines/{engineName}")]
    public async Task<IActionResult> ResolveWssEngine(string engineName)
    {
        var result = await _dispatcher.DispatchAsync("wss.engine.resolve", new Dictionary<string, object>
        {
            ["engineName"] = engineName
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Instance Registry (Phase 2.1.9) ---

    [HttpGet("wss/instances")]
    public async Task<IActionResult> GetWssInstances()
    {
        var result = await _dispatcher.DispatchAsync("wss.instance.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/instances/{instanceId}")]
    public async Task<IActionResult> GetWssInstance(string instanceId)
    {
        var result = await _dispatcher.DispatchAsync("wss.instance.get", new Dictionary<string, object>
        {
            ["instanceId"] = instanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/instances/create")]
    public async Task<IActionResult> CreateWssInstance([FromBody] DebugCreateWssInstanceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.instance.create", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["version"] = dto.Version,
            ["context"] = dto.Context!
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/instances/update")]
    public async Task<IActionResult> UpdateWssInstance([FromBody] DebugUpdateWssInstanceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.instance.update", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["currentStep"] = dto.CurrentStep,
            ["status"] = dto.Status
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("wss/instances/{instanceId}")]
    public async Task<IActionResult> RemoveWssInstance(string instanceId)
    {
        var result = await _dispatcher.DispatchAsync("wss.instance.remove", new Dictionary<string, object>
        {
            ["instanceId"] = instanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Workflow State Store (Phase 2.1.10) ---

    [HttpGet("wss/state")]
    public async Task<IActionResult> GetWssActiveStates()
    {
        var result = await _dispatcher.DispatchAsync("wss.state.list", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/state/{instanceId}")]
    public async Task<IActionResult> GetWssState(string instanceId)
    {
        var result = await _dispatcher.DispatchAsync("wss.state.get", new Dictionary<string, object>
        {
            ["instanceId"] = instanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/state/save")]
    public async Task<IActionResult> SaveWssState([FromBody] DebugSaveWssStateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.state.save", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["workflowId"] = dto.WorkflowId,
            ["workflowVersion"] = dto.WorkflowVersion,
            ["executionContext"] = dto.ExecutionContext!
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/state/update")]
    public async Task<IActionResult> UpdateWssState([FromBody] DebugUpdateWssStateDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.state.update", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["currentStep"] = dto.CurrentStep,
            ["status"] = dto.Status
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("wss/state/{instanceId}")]
    public async Task<IActionResult> DeleteWssState(string instanceId)
    {
        var result = await _dispatcher.DispatchAsync("wss.state.delete", new Dictionary<string, object>
        {
            ["instanceId"] = instanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Workflow Event Router (Phase 2.1.11) ---

    [HttpPost("wss/events/publish")]
    public async Task<IActionResult> PublishWssEvent([FromBody] DebugPublishWssEventDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.events.publish", new Dictionary<string, object>
        {
            ["eventType"] = dto.EventType,
            ["workflowId"] = dto.WorkflowId,
            ["instanceId"] = dto.InstanceId,
            ["payload"] = dto.Payload!
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("wss/events/types")]
    public async Task<IActionResult> GetWssEventTypes()
    {
        var result = await _dispatcher.DispatchAsync("wss.events.types", new Dictionary<string, object>());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Workflow Retry Policy Engine (Phase 2.1.12) ---

    [HttpGet("wss/retry/{instanceId}/{stepId}")]
    public async Task<IActionResult> GetWssRetryCount(string instanceId, string stepId)
    {
        var result = await _dispatcher.DispatchAsync("wss.retry.get", new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/retry/register")]
    public async Task<IActionResult> RegisterWssRetryAttempt([FromBody] DebugRetryRegisterDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.retry.register", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/retry/reset")]
    public async Task<IActionResult> ResetWssRetryCount([FromBody] DebugRetryResetDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.retry.reset", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Workflow Timeout Engine (Phase 2.1.13) ---

    [HttpGet("wss/timeouts")]
    public IActionResult GetWssTimeouts()
    {
        return Ok(new
        {
            endpoints = new[]
            {
                "POST /dev/wss/timeouts/register",
                "POST /dev/wss/timeouts/check",
                "POST /dev/wss/timeouts/clear"
            }
        });
    }

    [HttpPost("wss/timeouts/register")]
    public async Task<IActionResult> RegisterWssTimeout([FromBody] DebugTimeoutRegisterDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.timeout.register", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId,
            ["timeoutSeconds"] = dto.TimeoutSeconds
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/timeouts/check")]
    public async Task<IActionResult> CheckWssTimeout([FromBody] DebugTimeoutCheckDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.timeout.check", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId!
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/timeouts/clear")]
    public async Task<IActionResult> ClearWssTimeout([FromBody] DebugTimeoutClearDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.timeout.clear", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // --- WSS Workflow Instance Lifecycle Engine (Phase 2.1.14) ---

    [HttpPost("wss/workflows/start")]
    public async Task<IActionResult> StartWssWorkflow([FromBody] DebugLifecycleStartDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.lifecycle.start", new Dictionary<string, object>
        {
            ["workflowId"] = dto.WorkflowId,
            ["version"] = dto.Version,
            ["context"] = dto.Context!
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/workflows/advance")]
    public async Task<IActionResult> AdvanceWssWorkflow([FromBody] DebugLifecycleInstanceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.lifecycle.advance", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/workflows/complete")]
    public async Task<IActionResult> CompleteWssWorkflow([FromBody] DebugLifecycleStepDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.lifecycle.complete", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId!
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/workflows/fail")]
    public async Task<IActionResult> FailWssWorkflow([FromBody] DebugLifecycleFailDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.lifecycle.fail", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId,
            ["stepId"] = dto.StepId,
            ["reason"] = dto.Reason
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("wss/workflows/terminate")]
    public async Task<IActionResult> TerminateWssWorkflow([FromBody] DebugLifecycleInstanceDto dto)
    {
        var result = await _dispatcher.DispatchAsync("wss.lifecycle.terminate", new Dictionary<string, object>
        {
            ["instanceId"] = dto.InstanceId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
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
public sealed record DebugPublishWssEventDto(string EventType, string WorkflowId, string InstanceId, Dictionary<string, object>? Payload);
public sealed record DebugRetryRegisterDto(string InstanceId, string StepId);
public sealed record DebugRetryResetDto(string InstanceId, string StepId);
public sealed record DebugTimeoutRegisterDto(string InstanceId, string StepId, int TimeoutSeconds);
public sealed record DebugTimeoutCheckDto(string InstanceId, string? StepId);
public sealed record DebugTimeoutClearDto(string InstanceId, string StepId);
public sealed record DebugLifecycleStartDto(string WorkflowId, string Version, Dictionary<string, object>? Context);
public sealed record DebugLifecycleInstanceDto(string InstanceId);
public sealed record DebugLifecycleStepDto(string InstanceId, string? StepId);
public sealed record DebugLifecycleFailDto(string InstanceId, string StepId, string Reason);
