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
        IdentityAuditStore identityAuditStore)
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
