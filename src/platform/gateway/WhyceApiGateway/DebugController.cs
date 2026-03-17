namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.ArchitectureGuardrails.Enforcement;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.EventFabricRuntime.Bus;
using Whycespace.EngineRuntime.Registry;
using Whycespace.WorkflowRuntime;
using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;
using Whycespace.Systems.Midstream.WSS.Mapping;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Domain.Core.Cluster.Bootstrap;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Services;
using Whycespace.ClusterTemplatePlatform;
using Whycespace.Domain.Core.Economic;
using Whycespace.Runtime.Validation.Runners;
using Whycespace.Runtime.Validation.Pipelines;
using Whycespace.Runtime.EngineManifest.Registry;
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
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;
using Whycespace.Systems.Upstream.WhyceChain.Stores;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.Upstream.Governance.Registry;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Registry;
using Whycespace.EventReplay.Governance.Engine;
using Whycespace.EventReplay.Governance.Models;
using Whycespace.EventObservability.Metrics.Engine;
using Whycespace.Reliability.Isolation.Monitor;
using Whycespace.Reliability.Isolation.Registry;
using Whycespace.Reliability.Isolation.Engine;
using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.Governance.Evidence.Models;
using Whycespace.Systems.Upstream.WhyceChain.Ledger;
using Whycespace.Contracts.Evidence;
using Whycespace.Domain.Core.Economic;
using Whycespace.Engines.T2E.Capital;
using Whycespace.Systems.Midstream.Economics.CapitalLedger;
using Whycespace.Engines.T3I.Capital;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

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
    private readonly IGuardianRegistry _guardianRegistry;
    private readonly GovernanceRoleStore _governanceRoleStore;
    private readonly EventReplayGovernanceEngine _replayGovernanceEngine;
    private readonly EventObservabilityEngine _eventObservabilityEngine;
    private readonly WorkerHealthMonitor _workerHealthMonitor;
    private readonly PartitionHealthRegistry _partitionHealthRegistry;
    private readonly PartitionCircuitBreakerEngine _partitionCircuitBreakerEngine;
    private readonly BlockBuilderEngine _blockBuilderEngine;
    private readonly IntegrityVerificationEngine _integrityVerificationEngine;
    private readonly IGovernanceProposalRegistry _governanceProposalRegistry;
    private readonly GovernanceProposalTypeEngine _proposalTypeEngine;
    private readonly GovernanceProposalEngine _governanceProposalEngine;
    private readonly VotingEngine _votingEngine;
    private readonly GovernanceVoteStore _governanceVoteStore;
    private readonly GovernanceDomainScopeEngine _domainScopeEngine;
    private readonly GovernanceEvidenceRecorder _evidenceRecorder;
    private readonly GovernanceEmergencyEngine _emergencyEngine;
    private readonly GovernanceEmergencyStore _emergencyStore;
    private readonly ICapitalEvidenceRecorder _capitalEvidenceRecorder;
    private readonly ICapitalRegistry _capitalRegistry;
    private readonly CapitalPolicyEnforcementAdapter _capitalPolicyAdapter;
    private readonly CapitalLedgerStore _capitalLedgerStore;
    private readonly CapitalLifecycleEngine _capitalLifecycleEngine;

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
        IGuardianRegistry guardianRegistry,
        GovernanceRoleStore governanceRoleStore,
        EventReplayGovernanceEngine replayGovernanceEngine,
        EventObservabilityEngine eventObservabilityEngine,
        WorkerHealthMonitor workerHealthMonitor,
        PartitionHealthRegistry partitionHealthRegistry,
        PartitionCircuitBreakerEngine partitionCircuitBreakerEngine,
        BlockBuilderEngine blockBuilderEngine,
        IntegrityVerificationEngine integrityVerificationEngine,
        IGovernanceProposalRegistry governanceProposalRegistry,
        GovernanceProposalTypeEngine proposalTypeEngine,
        GovernanceProposalEngine governanceProposalEngine,
        VotingEngine votingEngine,
        GovernanceVoteStore governanceVoteStore,
        GovernanceDomainScopeEngine domainScopeEngine,
        GovernanceEvidenceRecorder evidenceRecorder,
        GovernanceEmergencyEngine emergencyEngine,
        GovernanceEmergencyStore emergencyStore,
        ICapitalEvidenceRecorder capitalEvidenceRecorder,
        ICapitalRegistry capitalRegistry,
        CapitalPolicyEnforcementAdapter capitalPolicyAdapter,
        CapitalLedgerStore capitalLedgerStore,
        CapitalLifecycleEngine capitalLifecycleEngine)
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
        _guardianRegistry = guardianRegistry;
        _governanceRoleStore = governanceRoleStore;
        _replayGovernanceEngine = replayGovernanceEngine;
        _eventObservabilityEngine = eventObservabilityEngine;
        _workerHealthMonitor = workerHealthMonitor;
        _partitionHealthRegistry = partitionHealthRegistry;
        _partitionCircuitBreakerEngine = partitionCircuitBreakerEngine;
        _blockBuilderEngine = blockBuilderEngine;
        _integrityVerificationEngine = integrityVerificationEngine;
        _governanceProposalRegistry = governanceProposalRegistry;
        _proposalTypeEngine = proposalTypeEngine;
        _governanceProposalEngine = governanceProposalEngine;
        _votingEngine = votingEngine;
        _governanceVoteStore = governanceVoteStore;
        _domainScopeEngine = domainScopeEngine;
        _evidenceRecorder = evidenceRecorder;
        _emergencyEngine = emergencyEngine;
        _emergencyStore = emergencyStore;
        _capitalEvidenceRecorder = capitalEvidenceRecorder;
        _capitalRegistry = capitalRegistry;
        _capitalPolicyAdapter = capitalPolicyAdapter;
        _capitalLedgerStore = capitalLedgerStore;
        _capitalLifecycleEngine = capitalLifecycleEngine;
    }

    [HttpGet("workflows")]
    public IActionResult GetWorkflows() => Ok(_stateStore.GetAll());

    [HttpGet("engines")]
    public IActionResult GetEngines() => Ok(_engineRegistry.ListEngines());

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

    [HttpPost("events/replay")]
    public IActionResult ReplayEventGoverned([FromBody] DebugReplayGovernedDto dto)
    {
        var request = new ReplayRequest(dto.EventId, dto.SourceTopic ?? "", dto.Payload ?? "", dto.ReplayCount);
        var decision = _replayGovernanceEngine.EvaluateReplay(request);
        return Ok(decision);
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
        var engines = _engineRegistry.ListEngines();
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

    [HttpGet("events/metrics")]
    public IActionResult GetEventMetrics()
    {
        var snapshot = _eventObservabilityEngine.GetSnapshot();
        return Ok(new
        {
            eventsProcessed = snapshot.EventMetrics.EventsProcessed,
            eventsSucceeded = snapshot.EventMetrics.EventsSucceeded,
            eventsFailed = snapshot.EventMetrics.EventsFailed
        });
    }

    [HttpGet("events/metrics/failures")]
    public IActionResult GetEventFailureMetrics()
    {
        var snapshot = _eventObservabilityEngine.GetSnapshot();
        return Ok(new
        {
            retryAttempts = snapshot.FailureMetrics.RetryAttempts,
            deadLetterEvents = snapshot.FailureMetrics.DeadLetterEvents,
            engineFailures = snapshot.FailureMetrics.EngineFailures,
            infrastructureFailures = snapshot.FailureMetrics.InfrastructureFailures
        });
    }

    [HttpGet("events/metrics/replay")]
    public IActionResult GetEventReplayMetrics()
    {
        var snapshot = _eventObservabilityEngine.GetSnapshot();
        return Ok(new
        {
            replayAttempts = snapshot.ReplayMetrics.ReplayAttempts,
            replaySucceeded = snapshot.ReplayMetrics.ReplaySucceeded,
            replayRejected = snapshot.ReplayMetrics.ReplayRejected
        });
    }

    [HttpGet("events/metrics/partitions")]
    public IActionResult GetEventPartitionMetrics()
    {
        var snapshot = _eventObservabilityEngine.GetSnapshot();
        return Ok(new
        {
            partitionsHealthy = snapshot.PartitionMetrics.PartitionsHealthy,
            partitionsDegraded = snapshot.PartitionMetrics.PartitionsDegraded,
            partitionsCircuitOpen = snapshot.PartitionMetrics.PartitionsCircuitOpen
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
        var config = new Whycespace.Systems.Upstream.WhycePolicy.Models.PolicyRolloutConfig(
            dto.PolicyId, dto.Version,
            Enum.Parse<Whycespace.Systems.Upstream.WhycePolicy.Models.PolicyRolloutStrategy>(dto.Strategy, ignoreCase: true),
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

    // WhyceChain — Integrity Verification (Phase 2.0.45)

    [HttpGet("chain/integrity")]
    public IActionResult VerifyChainIntegrity()
    {
        var entries = _chainLedgerStore.GetAllEntries().ToList();
        var latestBlock = _chainBlockStore.GetLatestBlock();
        var blocks = new List<Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock>();

        if (latestBlock is not null)
        {
            for (long i = 0; i <= latestBlock.BlockNumber; i++)
            {
                try { blocks.Add(_chainBlockStore.GetBlock(i)); }
                catch (KeyNotFoundException) { break; }
            }
        }

        var command = new Whycespace.Systems.Upstream.WhyceChain.Models.IntegrityVerificationCommand(
            entries,
            blocks,
            MerkleProof: null,
            TraceId: Guid.NewGuid().ToString(),
            CorrelationId: Guid.NewGuid().ToString(),
            Timestamp: DateTimeOffset.UtcNow);

        var result = _integrityVerificationEngine.Execute(command);

        return Ok(new
        {
            result.LedgerValid,
            result.BlockChainValid,
            result.MerkleRootValid,
            result.MerkleProofValid,
            result.TamperedEntries,
            result.VerificationTimestamp,
            result.TraceId,
            entriesVerified = entries.Count,
            blocksVerified = blocks.Count
        });
    }

    [HttpPost("chain/integrity/proof")]
    public IActionResult VerifyMerkleProof([FromBody] DebugVerifyMerkleProofDto dto)
    {
        var proof = new Whycespace.Systems.Upstream.WhyceChain.Models.MerkleProof(
            dto.RootHash, dto.LeafHash, dto.ProofPath);

        var command = new Whycespace.Systems.Upstream.WhyceChain.Models.IntegrityVerificationCommand(
            Array.Empty<Whycespace.Systems.Upstream.WhyceChain.Models.ChainLedgerEntry>(),
            Array.Empty<Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock>(),
            proof,
            TraceId: Guid.NewGuid().ToString(),
            CorrelationId: Guid.NewGuid().ToString(),
            Timestamp: DateTimeOffset.UtcNow);

        var result = _integrityVerificationEngine.Execute(command);

        return Ok(new
        {
            result.MerkleProofValid,
            result.TraceId,
            result.VerificationTimestamp
        });
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

    // Governance — Guardian Registry v2 (Phase 2.0.54 — GuardianRecord queries)

    [HttpGet("governance/guardians/role/{role}")]
    public IActionResult GetGuardiansByRole(GuardianRole role)
    {
        var guardians = _guardianRegistry.GetGuardiansByRole(role);
        return Ok(new { guardians });
    }

    [HttpGet("governance/guardians/domain/{domain}")]
    public IActionResult GetGuardiansByDomain(string domain)
    {
        var guardians = _guardianRegistry.GetGuardiansByDomain(domain);
        return Ok(new { guardians });
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

    // Governance — Proposal Type Engine (Phase 2.0.59)

    [HttpPost("governance/proposal-type/register")]
    public IActionResult RegisterProposalType([FromBody] DebugRegisterProposalTypeDto dto)
    {
        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(),
            dto.ProposalType,
            dto.Description,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, _) = _proposalTypeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("governance/proposal-type/deactivate")]
    public IActionResult DeactivateProposalType([FromBody] DebugDeactivateProposalTypeDto dto)
    {
        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(),
            dto.ProposalType,
            dto.Reason,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, _) = _proposalTypeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("governance/proposal-type/validate")]
    public IActionResult ValidateProposalType([FromBody] DebugValidateProposalTypeDto dto)
    {
        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(),
            dto.ProposalType,
            dto.AuthorityDomain,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, _) = _proposalTypeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpGet("governance/proposal-types")]
    public IActionResult GetProposalTypes()
    {
        return Ok(_proposalTypeEngine.ListTypes());
    }

    // Governance — Domain Scope Engine (Phase 2.0.60)

    [HttpPost("governance/domain/register")]
    public IActionResult RegisterDomainScope([FromBody] DebugRegisterDomainScopeDto dto)
    {
        var command = new RegisterDomainScopeCommand(
            Guid.NewGuid(),
            dto.AuthorityDomain,
            dto.Description,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, domainEvent) = _domainScopeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { result, domainEvent });
    }

    [HttpPost("governance/domain/deactivate")]
    public IActionResult DeactivateDomainScope([FromBody] DebugDeactivateDomainScopeDto dto)
    {
        var command = new DeactivateDomainScopeCommand(
            Guid.NewGuid(),
            dto.AuthorityDomain,
            dto.Reason,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, domainEvent) = _domainScopeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { result, domainEvent });
    }

    [HttpPost("governance/domain/validate")]
    public IActionResult ValidateDomainScope([FromBody] DebugValidateDomainScopeDto dto)
    {
        var command = new ValidateDomainScopeCommand(
            Guid.NewGuid(),
            dto.ProposalId,
            dto.AuthorityDomain,
            dto.ProposalType,
            dto.GuardianId,
            DateTime.UtcNow);

        var (result, domainEvent) = _domainScopeEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { result, domainEvent });
    }

    [HttpGet("governance/domains")]
    public IActionResult GetDomainScopes()
    {
        return Ok(_domainScopeEngine.ListDomains());
    }

    // Governance — Delegation Engine (Phase 2.0.56)

    [HttpPost("governance/delegation/create")]
    public async Task<IActionResult> CreateGovernanceDelegation([FromBody] DebugCreateDelegationDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.delegation.create", new Dictionary<string, object>
        {
            ["delegationId"] = dto.DelegationId,
            ["fromGuardian"] = dto.FromGuardian,
            ["toGuardian"] = dto.ToGuardian,
            ["roleScope"] = dto.RoleScope,
            ["startTime"] = dto.StartTime,
            ["endTime"] = dto.EndTime
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("governance/delegation/revoke")]
    public async Task<IActionResult> RevokeGovernanceDelegation([FromBody] DebugRevokeDelegationDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.delegation.revoke", new Dictionary<string, object>
        {
            ["delegationId"] = dto.DelegationId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("governance/delegation/{delegationId}")]
    public async Task<IActionResult> GetGovernanceDelegation(string delegationId)
    {
        var result = await _dispatcher.DispatchAsync("governance.delegation.get", new Dictionary<string, object>
        {
            ["delegationId"] = delegationId
        });

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("governance/delegations/guardian/{guardianId}")]
    public async Task<IActionResult> GetGuardianDelegations(string guardianId)
    {
        var result = await _dispatcher.DispatchAsync("governance.delegation.listByGuardian", new Dictionary<string, object>
        {
            ["guardianId"] = guardianId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // Governance — Quorum Engine (Phase 2.0.62)

    [HttpPost("governance/quorum/evaluate")]
    public IActionResult EvaluateQuorum([FromBody] DebugEvaluateQuorumDto dto)
    {
        var command = new Engines.T0U.Governance.Commands.EvaluateQuorumCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: dto.ProposalId,
            TotalEligibleGuardians: dto.TotalEligibleGuardians,
            VotesCast: dto.VotesCast,
            VotesApprove: dto.VotesApprove,
            VotesReject: dto.VotesReject,
            VotesAbstain: dto.VotesAbstain,
            RequiredParticipationPercentage: dto.RequiredParticipationPercentage,
            RequiredApprovalPercentage: dto.RequiredApprovalPercentage,
            Timestamp: DateTime.UtcNow);

        var quorumEngine = new Engines.T0U.Governance.QuorumEngine();

        var (result, evaluatedEvent, outcomeEvent) = quorumEngine.Execute(command);

        return Ok(new
        {
            result.Success,
            result.ProposalId,
            result.ParticipationPercentage,
            result.ApprovalPercentage,
            result.QuorumMet,
            result.Message,
            result.ExecutedAt,
            evaluatedEvent,
            outcomeEvent
        });
    }

    [HttpGet("governance/quorum/{proposalId}")]
    public IActionResult GetQuorumStatus(Guid proposalId)
    {
        return Ok(new
        {
            proposalId,
            message = "Quorum status lookup — requires projection integration",
            timestamp = DateTime.UtcNow
        });
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

    [HttpGet("runtime/workers/health")]
    public IActionResult GetWorkerHealth()
    {
        var workers = _workerHealthMonitor.GetAllWorkerHealth();
        return Ok(workers.Select(w => new
        {
            workerId = w.Key,
            status = w.Value.ToString()
        }));
    }

    [HttpGet("runtime/partitions/health")]
    public IActionResult GetPartitionHealth()
    {
        var partitions = _partitionHealthRegistry.GetAllPartitionHealth();
        var failures = _partitionHealthRegistry.GetAllFailureCounts();
        return Ok(partitions.Select(p => new
        {
            partition = p.Key,
            status = p.Value.ToString(),
            failures = failures.GetValueOrDefault(p.Key, 0)
        }));
    }

    [HttpGet("runtime/circuit-breakers")]
    public IActionResult GetCircuitBreakers()
    {
        var states = _partitionHealthRegistry.GetAllCircuitStates();
        var failures = _partitionHealthRegistry.GetAllFailureCounts();
        return Ok(states.Select(s => new
        {
            partition = s.Key,
            state = s.Value.ToString(),
            failures = failures.GetValueOrDefault(s.Key, 0)
        }));
    }

    // WhyceChain — Block Builder (Phase 2.0.46)

    [HttpPost("chain/block/build")]
    public IActionResult BuildChainBlock([FromBody] DebugBuildBlockDto dto)
    {
        var entries = dto.EntryHashes.Select((hash, i) =>
            new ChainLedgerEntry(
                EntryId: Guid.NewGuid(),
                EntryType: "DebugEntry",
                AggregateId: "debug",
                SequenceNumber: i,
                PayloadHash: hash,
                MetadataHash: hash,
                PreviousEntryHash: i == 0 ? "genesis" : dto.EntryHashes[i - 1],
                EntryHash: hash,
                Timestamp: DateTimeOffset.UtcNow,
                TraceId: dto.TraceId ?? Guid.NewGuid().ToString(),
                CorrelationId: dto.CorrelationId ?? Guid.NewGuid().ToString(),
                EventVersion: 1)).ToList();

        var command = new BlockBuilderCommand(
            BlockHeight: dto.BlockHeight,
            PreviousBlockHash: dto.PreviousBlockHash ?? "genesis",
            LedgerEntries: entries,
            TraceId: dto.TraceId ?? Guid.NewGuid().ToString(),
            CorrelationId: dto.CorrelationId ?? Guid.NewGuid().ToString(),
            Timestamp: DateTime.UtcNow);

        var result = _blockBuilderEngine.Execute(command);

        return Ok(new
        {
            result.Block.BlockId,
            result.Block.BlockHeight,
            result.Block.PreviousBlockHash,
            result.BlockHash,
            result.MerkleRoot,
            result.EntryCount,
            result.GeneratedAt,
            result.TraceId,
            Entries = result.Block.Entries.Select(e => new
            {
                e.EntryId,
                e.EntryHash,
                e.SequenceNumber
            })
        });
    }

    [HttpGet("governance/proposals")]
    public IActionResult GetGovernanceProposals()
    {
        return Ok(_governanceProposalRegistry.GetProposals());
    }

    [HttpGet("governance/proposals/{id:guid}")]
    public IActionResult GetGovernanceProposal(Guid id)
    {
        var proposal = _governanceProposalRegistry.GetProposal(id);
        if (proposal is null)
            return NotFound($"Proposal not found: {id}");
        return Ok(proposal);
    }

    [HttpGet("governance/proposals/status/{status}")]
    public IActionResult GetGovernanceProposalsByStatus(GovernanceProposalStatus status)
    {
        return Ok(_governanceProposalRegistry.GetProposalsByStatus(status));
    }

    [HttpGet("governance/proposals/type/{type}")]
    public IActionResult GetGovernanceProposalsByType(Whycespace.Systems.Upstream.Governance.Proposals.Models.GovernanceProposalType type)
    {
        return Ok(_governanceProposalRegistry.GetProposalsByType(type));
    }

    [HttpPost("governance/proposals/register")]
    public IActionResult RegisterGovernanceProposal([FromBody] DebugRegisterGovernanceProposalDto dto)
    {
        var record = new GovernanceProposalRecord(
            Guid.NewGuid(),
            dto.Title,
            dto.Description,
            dto.Type,
            GovernanceProposalStatus.Draft,
            dto.AuthorityDomain,
            dto.ProposedByGuardianId,
            DateTime.UtcNow,
            null,
            null,
            null,
            dto.Metadata ?? new Dictionary<string, string>());

        _governanceProposalRegistry.RegisterProposal(record);
        return Ok(record);
    }

    [HttpPost("governance/vote/cast")]
    public IActionResult CastGovernanceVote([FromBody] DebugCastVoteDto dto)
    {
        var command = new CastVoteCommand(
            Guid.NewGuid().ToString(),
            dto.ProposalId,
            dto.GuardianId,
            dto.VoteDecision,
            dto.VoteWeight,
            DateTime.UtcNow);

        var (result, _) = _votingEngine.Execute(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("governance/vote/withdraw")]
    public IActionResult WithdrawGovernanceVote([FromBody] DebugWithdrawVoteDto dto)
    {
        var command = new WithdrawVoteCommand(
            Guid.NewGuid().ToString(),
            dto.ProposalId,
            dto.GuardianId,
            dto.Reason,
            DateTime.UtcNow);

        var (result, _) = _votingEngine.Execute(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("governance/vote/validate")]
    public IActionResult ValidateGovernanceVote([FromBody] DebugValidateVoteDto dto)
    {
        var command = new ValidateVoteCommand(
            Guid.NewGuid().ToString(),
            dto.ProposalId,
            dto.GuardianId,
            dto.VoteDecision,
            DateTime.UtcNow);

        var (result, _) = _votingEngine.Execute(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("governance/votes/{proposalId}")]
    public IActionResult GetGovernanceVotes(string proposalId)
    {
        var votes = _governanceVoteStore.GetByProposal(proposalId);
        return Ok(votes);
    }

    // Governance — Proposal Engine (Phase 2.0.58)

    [HttpPost("governance/proposal/create")]
    public IActionResult CreateGovernanceProposal([FromBody] DebugCreateGovernanceProposalCommandDto dto)
    {
        var command = new CreateGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: dto.ProposalId ?? Guid.NewGuid(),
            ProposalTitle: dto.ProposalTitle,
            ProposalDescription: dto.ProposalDescription,
            ProposalType: dto.ProposalType,
            AuthorityDomain: dto.AuthorityDomain,
            ProposedByGuardianId: dto.ProposedByGuardianId,
            Metadata: dto.Metadata ?? new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _governanceProposalEngine.Execute(command);

        return result.Success
            ? Ok(new { result, domainEvent })
            : BadRequest(new { result.Message });
    }

    [HttpPost("governance/proposal/submit")]
    public IActionResult SubmitGovernanceProposal([FromBody] DebugSubmitGovernanceProposalDto dto)
    {
        var command = new SubmitGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: dto.ProposalId,
            SubmittedByGuardianId: dto.SubmittedByGuardianId,
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _governanceProposalEngine.Execute(command);

        return result.Success
            ? Ok(new { result, domainEvent })
            : BadRequest(new { result.Message });
    }

    [HttpPost("governance/proposal/cancel")]
    public IActionResult CancelGovernanceProposal([FromBody] DebugCancelGovernanceProposalDto dto)
    {
        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: dto.ProposalId,
            CancelledByGuardianId: dto.CancelledByGuardianId,
            Reason: dto.Reason,
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _governanceProposalEngine.Execute(command);

        return result.Success
            ? Ok(new { result, domainEvent })
            : BadRequest(new { result.Message });
    }

    [HttpGet("governance/proposal/{proposalId}")]
    public IActionResult GetGovernanceProposalById(Guid proposalId)
    {
        var proposal = _governanceProposalRegistry.GetProposal(proposalId);
        if (proposal is null)
            return NotFound(new { error = $"Proposal not found: {proposalId}" });
        return Ok(proposal);
    }

    // Governance — Governance Workflow Engine (Phase 2.0.64)

    [HttpPost("governance/workflow/start")]
    public async Task<IActionResult> StartGovernanceWorkflow([FromBody] DebugStartGovernanceWorkflowDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.workflow.start", new Dictionary<string, object>
        {
            ["proposalId"] = dto.ProposalId.ToString(),
            ["startedByGuardianId"] = dto.StartedByGuardianId.ToString()
        });

        return result.Success ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("governance/workflow/advance")]
    public async Task<IActionResult> AdvanceGovernanceWorkflow([FromBody] DebugAdvanceGovernanceWorkflowDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.workflow.advance", new Dictionary<string, object>
        {
            ["proposalId"] = dto.ProposalId.ToString(),
            ["currentStep"] = dto.CurrentStep,
            ["nextStep"] = dto.NextStep,
            ["triggeredBy"] = dto.TriggeredBy.ToString()
        });

        return result.Success ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("governance/workflow/complete")]
    public async Task<IActionResult> CompleteGovernanceWorkflow([FromBody] DebugCompleteGovernanceWorkflowDto dto)
    {
        var result = await _dispatcher.DispatchAsync("governance.workflow.complete", new Dictionary<string, object>
        {
            ["proposalId"] = dto.ProposalId.ToString(),
            ["completedBy"] = dto.CompletedBy.ToString()
        });

        return result.Success ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpGet("governance/workflow/{proposalId}")]
    public async Task<IActionResult> GetGovernanceWorkflow(Guid proposalId)
    {
        var result = await _dispatcher.DispatchAsync("governance.workflow.get", new Dictionary<string, object>
        {
            ["proposalId"] = proposalId.ToString()
        });

        return result.Success ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    // Governance — Evidence Recorder (Phase 2.0.67)

    [HttpPost("governance/evidence/record")]
    public IActionResult RecordGovernanceEvidence([FromBody] DebugRecordGovernanceEvidenceDto dto)
    {
        var command = new RecordGovernanceEvidenceCommand(
            Guid.NewGuid(),
            dto.ProposalId,
            dto.EventReferenceId,
            dto.EvidenceType,
            dto.RecordedByGuardianId,
            dto.EvidencePayload,
            DateTime.UtcNow);

        var result = _evidenceRecorder.Execute(command);

        return result.Success
            ? Ok(result)
            : BadRequest(new { message = result.Message });
    }

    [HttpGet("governance/evidence/{evidenceId:guid}")]
    public IActionResult GetGovernanceEvidence(Guid evidenceId)
    {
        return Ok(new { evidenceId, message = "Evidence lookup requires projection — not yet implemented." });
    }

    [HttpGet("governance/evidence/proposal/{proposalId:guid}")]
    public IActionResult GetGovernanceEvidenceByProposal(Guid proposalId)
    {
        return Ok(new { proposalId, message = "Evidence-by-proposal lookup requires projection — not yet implemented." });
    }

    // Governance — Emergency Engine (Phase 2.0.66)

    [HttpPost("governance/emergency/trigger")]
    public IActionResult TriggerEmergencyAction([FromBody] DebugTriggerEmergencyDto dto)
    {
        var command = new TriggerEmergencyActionCommand(
            CommandId: Guid.NewGuid(),
            EmergencyActionId: dto.EmergencyActionId,
            EmergencyType: dto.EmergencyType,
            TargetDomain: dto.TargetDomain,
            TriggeredByGuardianId: dto.TriggeredByGuardianId,
            Reason: dto.Reason,
            Timestamp: DateTime.UtcNow);

        var result = _emergencyEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            result.EmergencyActionId,
            emergencyType = result.EmergencyType.ToString(),
            emergencyStatus = result.EmergencyStatus.ToString(),
            result.TargetDomain,
            result.Message,
            result.ExecutedAt
        });
    }

    [HttpPost("governance/emergency/revoke")]
    public IActionResult RevokeEmergencyAction([FromBody] DebugRevokeEmergencyDto dto)
    {
        var command = new RevokeEmergencyActionCommand(
            CommandId: Guid.NewGuid(),
            EmergencyActionId: dto.EmergencyActionId,
            RevokedByGuardianId: dto.RevokedByGuardianId,
            Reason: dto.Reason,
            Timestamp: DateTime.UtcNow);

        var result = _emergencyEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            result.EmergencyActionId,
            emergencyType = result.EmergencyType.ToString(),
            emergencyStatus = result.EmergencyStatus.ToString(),
            result.TargetDomain,
            result.Message,
            result.ExecutedAt
        });
    }

    [HttpPost("governance/emergency/validate")]
    public IActionResult ValidateEmergencyAction([FromBody] DebugValidateEmergencyDto dto)
    {
        var command = new ValidateEmergencyActionCommand(
            CommandId: Guid.NewGuid(),
            EmergencyActionId: dto.EmergencyActionId,
            GuardianId: dto.GuardianId,
            Timestamp: DateTime.UtcNow);

        var result = _emergencyEngine.Execute(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            result.EmergencyActionId,
            emergencyType = result.EmergencyType.ToString(),
            emergencyStatus = result.EmergencyStatus.ToString(),
            result.TargetDomain,
            result.Message,
            result.ExecutedAt
        });
    }

    [HttpGet("governance/emergency/{emergencyId}")]
    public IActionResult GetEmergency(string emergencyId)
    {
        var emergency = _emergencyStore.Get(emergencyId);
        if (emergency is null)
            return NotFound(new { error = $"Emergency not found: {emergencyId}" });

        return Ok(new
        {
            emergency.EmergencyId,
            type = emergency.Type.ToString(),
            status = emergency.Status.ToString(),
            emergency.TargetDomain,
            emergency.TriggeredBy,
            emergency.Reason,
            emergency.TriggeredAt,
            emergency.ResolvedAt
        });
    }

    [HttpGet("capital/ledger/{capitalId}")]
    public IActionResult GetCapitalLedger(Guid capitalId)
    {
        var entries = _capitalLedgerStore.GetEntriesByCapitalId(capitalId);

        if (entries.Count == 0)
            return NotFound(new { error = $"No ledger entries found for capital: {capitalId}" });

        return Ok(new
        {
            capitalId,
            entryCount = entries.Count,
            entries = entries.Select(e => new
            {
                e.EntryId,
                entryType = e.EntryType.ToString(),
                e.PoolId,
                e.InvestorIdentityId,
                e.ReferenceId,
                e.Amount,
                e.Currency,
                e.PreviousBalance,
                e.NewBalance,
                e.Timestamp,
                e.TraceId,
                e.CorrelationId
            })
        });
    }

    [HttpGet("capital/evidence/{capitalId:guid}")]
    public async Task<IActionResult> GetCapitalEvidence(Guid capitalId, [FromQuery] Guid? referenceId = null)
    {
        if (referenceId.HasValue)
        {
            var byReference = await _capitalEvidenceRecorder.GetEvidenceByReferenceIdAsync(referenceId.Value);
            return Ok(byReference);
        }

        var evidence = await _capitalEvidenceRecorder.GetEvidenceByCapitalIdAsync(capitalId);
        return Ok(evidence);
    }

    [HttpPost("capital/lifecycle/{capitalId:guid}")]
    public IActionResult TrackCapitalLifecycle(Guid capitalId, [FromBody] DebugTrackCapitalLifecycleDto dto)
    {
        var command = new TrackCapitalLifecycleCommand(
            CapitalId: capitalId,
            PreviousStage: dto.PreviousStage,
            NewStage: dto.NewStage,
            ReferenceId: dto.ReferenceId,
            TriggeredBy: dto.TriggeredBy,
            TriggeredAt: DateTimeOffset.UtcNow);

        var result = _capitalLifecycleEngine.TrackLifecycle(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            capitalId = result.Record!.CapitalId,
            previousStage = result.Record.PreviousStage.ToString(),
            currentStage = result.Record.CurrentStage.ToString(),
            referenceId = result.Record.ReferenceId,
            timestamp = result.Record.Timestamp.ToString("O")
        });
    }
}

public sealed record DebugRecordGovernanceEvidenceDto(Guid ProposalId, Guid EventReferenceId, EvidenceType EvidenceType, Guid RecordedByGuardianId, string EvidencePayload);

public sealed record DebugBuildBlockDto(
    long BlockHeight,
    List<string> EntryHashes,
    string? PreviousBlockHash,
    string? TraceId,
    string? CorrelationId);

public sealed record DebugRunWorkflowDto(string WorkflowName, Dictionary<string, object>? Context);
public sealed record DebugReplayEventDto(string EventType, Guid AggregateId, Dictionary<string, object>? Payload);
public sealed record DebugReplayGovernedDto(Guid EventId, string? SourceTopic, string? Payload, int ReplayCount);
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
public sealed record DebugCreateDelegationDto(string DelegationId, string FromGuardian, string ToGuardian, string RoleScope, DateTime StartTime, DateTime EndTime);
public sealed record DebugRevokeDelegationDto(string DelegationId);
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
public sealed record DebugVerifyMerkleProofDto(string RootHash, string LeafHash, List<string> ProofPath);
public sealed record DebugRegisterGovernanceProposalDto(string Title, string Description, Whycespace.Systems.Upstream.Governance.Proposals.Models.GovernanceProposalType Type, string AuthorityDomain, Guid ProposedByGuardianId, Dictionary<string, string>? Metadata);
public sealed record DebugEvaluateQuorumDto(Guid ProposalId, int TotalEligibleGuardians, int VotesCast, int VotesApprove, int VotesReject, int VotesAbstain, decimal RequiredParticipationPercentage, decimal RequiredApprovalPercentage);
public sealed record DebugRegisterProposalTypeDto(string ProposalType, string Description, Guid GuardianId);
public sealed record DebugDeactivateProposalTypeDto(string ProposalType, string Reason, Guid GuardianId);
public sealed record DebugValidateProposalTypeDto(string ProposalType, string AuthorityDomain, Guid GuardianId);
public sealed record DebugRegisterDomainScopeDto(string AuthorityDomain, string Description, Guid GuardianId);
public sealed record DebugDeactivateDomainScopeDto(string AuthorityDomain, string Reason, Guid GuardianId);
public sealed record DebugValidateDomainScopeDto(Guid ProposalId, string AuthorityDomain, ProposalType ProposalType, Guid GuardianId);
public sealed record DebugCastVoteDto(string ProposalId, string GuardianId, VoteType VoteDecision, int VoteWeight);
public sealed record DebugWithdrawVoteDto(string ProposalId, string GuardianId, string Reason);
public sealed record DebugValidateVoteDto(string ProposalId, string GuardianId, VoteType VoteDecision);
public sealed record DebugCreateGovernanceProposalCommandDto(string ProposalTitle, string ProposalDescription, ProposalType ProposalType, string AuthorityDomain, Guid ProposedByGuardianId, Guid? ProposalId = null, Dictionary<string, string>? Metadata = null);
public sealed record DebugSubmitGovernanceProposalDto(Guid ProposalId, Guid SubmittedByGuardianId);
public sealed record DebugCancelGovernanceProposalDto(Guid ProposalId, Guid CancelledByGuardianId, string Reason);
public sealed record DebugStartGovernanceWorkflowDto(Guid ProposalId, Guid StartedByGuardianId);
public sealed record DebugAdvanceGovernanceWorkflowDto(Guid ProposalId, string CurrentStep, string NextStep, Guid TriggeredBy);
public sealed record DebugCompleteGovernanceWorkflowDto(Guid ProposalId, Guid CompletedBy);
public sealed record DebugTriggerEmergencyDto(string EmergencyActionId, EmergencyType EmergencyType, string TargetDomain, string TriggeredByGuardianId, string Reason);
public sealed record DebugRevokeEmergencyDto(string EmergencyActionId, string RevokedByGuardianId, string Reason);
public sealed record DebugValidateEmergencyDto(string EmergencyActionId, string GuardianId);
public sealed record DebugTrackCapitalLifecycleDto(CapitalLifecycleStage PreviousStage, CapitalLifecycleStage NewStage, Guid ReferenceId, Guid TriggeredBy);
