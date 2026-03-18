namespace Whycespace.Platform.Simulation.Orchestration;

using Whycespace.Platform.Simulation.EventCapture;
using Whycespace.Platform.Simulation.Metrics;
using Whycespace.Platform.Simulation.Policy;
using Whycespace.Platform.Simulation.Trace;
using Whycespace.Platform.Simulation.Scenarios;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Shared.Envelopes;

/// <summary>
/// Cross-layer simulation orchestrator that coordinates workflow simulation,
/// event capture, policy evaluation, and trace collection without mutating real state.
/// Integrates upstream (WSS) through downstream (CWG/SPV) simulation paths.
/// </summary>
public sealed class SimulationOrchestrator
{
    private readonly SimulationPolicyAdapter _policyAdapter;
    private readonly SimulationEventCollector _eventCollector;
    private readonly SimulationTraceCollector _traceCollector;
    private readonly SimulationMetrics _metrics;

    public SimulationOrchestrator(
        SimulationPolicyAdapter policyAdapter,
        SimulationEventCollector eventCollector,
        SimulationTraceCollector traceCollector,
        SimulationMetrics metrics)
    {
        _policyAdapter = policyAdapter;
        _eventCollector = eventCollector;
        _traceCollector = traceCollector;
        _metrics = metrics;
    }

    /// <summary>
    /// Executes a full simulation cycle for a workflow payload:
    /// policy check -> step execution -> event capture -> trace collection.
    /// No state is mutated; events are captured, not published.
    /// </summary>
    public async Task<SimulationOrchestrationResult> ExecuteAsync(
        WorkflowPayload payload,
        Func<EngineInvocationEnvelope, Task<EngineResult>> dispatcher,
        CancellationToken ct = default)
    {
        var simulationId = Guid.NewGuid();
        var workflowId = payload.Graph.WorkflowId;

        _traceCollector.Record(simulationId, "OrchestrationStarted",
            $"Simulation started for workflow '{payload.WorkflowType}'");

        // Policy gate
        var policyResult = await _policyAdapter.EvaluateAsync(payload.WorkflowType, workflowId);
        _traceCollector.Record(simulationId, "PolicyEvaluated",
            $"Policy evaluation: {(policyResult.IsPermitted ? "permitted" : "denied")}");

        if (!policyResult.IsPermitted)
        {
            return new SimulationOrchestrationResult(
                SimulationId: simulationId,
                WorkflowType: payload.WorkflowType,
                Success: false,
                CapturedEvents: _eventCollector.GetEvents(simulationId),
                Traces: _traceCollector.GetTraces(simulationId),
                ErrorMessage: $"Policy denied simulation: {string.Join(", ", policyResult.Violations)}");
        }

        var context = new Dictionary<string, object>(payload.Context);
        var success = true;
        string? errorMessage = null;

        foreach (var step in payload.Graph.Steps)
        {
            if (ct.IsCancellationRequested) break;

            var envelope = new EngineInvocationEnvelope(
                Guid.NewGuid(), step.EngineName, workflowId,
                step.StepId, workflowId, context);

            _traceCollector.Record(simulationId, "EngineDispatch",
                $"Dispatching to engine '{step.EngineName}' for step '{step.StepId}'");

            var result = await dispatcher(envelope);

            if (!result.Success)
            {
                success = false;
                errorMessage = $"Engine '{step.EngineName}' failed at step '{step.StepId}'";
                _traceCollector.Record(simulationId, "EngineFailed", errorMessage);
                break;
            }

            // Capture events without publishing
            foreach (var ev in result.Events)
            {
                _eventCollector.Capture(simulationId, ev.EventType, ev.Payload,
                    $"whyce.events.{step.EngineName.ToLowerInvariant()}");
            }

            foreach (var kvp in result.Output)
                context[kvp.Key] = kvp.Value;

            _traceCollector.Record(simulationId, "StepCompleted",
                $"Step '{step.StepId}' completed with {result.Events.Count} events");
        }

        _traceCollector.Record(simulationId, "OrchestrationCompleted",
            $"Simulation completed: success={success}");

        return new SimulationOrchestrationResult(
            SimulationId: simulationId,
            WorkflowType: payload.WorkflowType,
            Success: success,
            CapturedEvents: _eventCollector.GetEvents(simulationId),
            Traces: _traceCollector.GetTraces(simulationId),
            ErrorMessage: errorMessage);
    }
}

public sealed record SimulationOrchestrationResult(
    Guid SimulationId,
    string WorkflowType,
    bool Success,
    IReadOnlyList<CapturedSimulationEvent> CapturedEvents,
    IReadOnlyList<SimulationTraceEntry> Traces,
    string? ErrorMessage = null);