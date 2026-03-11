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
        IdentityRegistry identityRegistry)
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
                identityId = identity.IdentityId.Value,
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
}

public sealed record DebugRunWorkflowDto(string WorkflowName, Dictionary<string, object>? Context);
public sealed record DebugReplayEventDto(string EventType, Guid AggregateId, Dictionary<string, object>? Payload);
public sealed record DebugRunSimulationDto(Guid ScenarioId);
public sealed record DebugGenerateClusterDto(string TemplateName);
