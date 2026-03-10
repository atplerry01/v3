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

    public DebugController(
        WorkflowStateStore stateStore,
        EngineRegistry engineRegistry,
        EventBus eventBus,
        WorkflowMapper workflowMapper,
        ClusterBootstrapper clusterBootstrapper,
        SimulationService simulationService)
    {
        _stateStore = stateStore;
        _engineRegistry = engineRegistry;
        _eventBus = eventBus;
        _workflowMapper = workflowMapper;
        _clusterBootstrapper = clusterBootstrapper;
        _simulationService = simulationService;
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
}

public sealed record DebugRunWorkflowDto(string WorkflowName, Dictionary<string, object>? Context);
public sealed record DebugReplayEventDto(string EventType, Guid AggregateId, Dictionary<string, object>? Payload);
public sealed record DebugRunSimulationDto(Guid ScenarioId);
