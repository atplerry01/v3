namespace Whycespace.SimulationRuntime.Engine;

using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Policy;
using Whycespace.SimulationRuntime.Exceptions;

public sealed class SimulationEngine
{
    private readonly SimulationPolicy _policy;

    public SimulationEngine(SimulationPolicy policy)
    {
        _policy = policy;
    }

    public SimulationExecutionResult Execute(SimulationCommand command)
    {
        if (!_policy.IsSimulationAllowed(command))
            throw new SimulationException($"Simulation denied by policy for command '{command.CommandType}'.");

        var context = new SimulationContext(
            SimulationId: Guid.NewGuid(),
            Command: command,
            StartedAt: DateTimeOffset.UtcNow);

        var capturedEvents = new List<SimulationEventRecord>();
        var traces = new List<SimulationTrace>();

        // Simulate command execution without mutating state
        traces.Add(new SimulationTrace(
            StepName: "CommandReceived",
            Description: $"Command '{command.CommandType}' received for simulation",
            Timestamp: DateTimeOffset.UtcNow));

        // Simulate engine resolution
        traces.Add(new SimulationTrace(
            StepName: "EngineResolution",
            Description: $"Resolved target engine for '{command.CommandType}'",
            Timestamp: DateTimeOffset.UtcNow));

        // Simulate execution — capture events instead of publishing
        var simulatedEvent = new SimulationEventRecord(
            EventId: Guid.NewGuid(),
            EventType: $"{command.CommandType}Completed",
            Payload: command.Payload,
            Timestamp: DateTimeOffset.UtcNow,
            WouldPublishToTopic: $"whyce.events.{command.CommandType.ToLowerInvariant()}");

        capturedEvents.Add(simulatedEvent);

        traces.Add(new SimulationTrace(
            StepName: "EventCaptured",
            Description: $"Captured event '{simulatedEvent.EventType}' (not published)",
            Timestamp: DateTimeOffset.UtcNow));

        var snapshot = new SimulationStateSnapshot(
            AffectedAggregates: command.AggregateId is not null ? new[] { command.AggregateId } : Array.Empty<string>(),
            CapturedEventCount: capturedEvents.Count,
            PolicyEvaluations: 1,
            SimulatedAt: DateTimeOffset.UtcNow);

        return new SimulationExecutionResult(
            SimulationId: context.SimulationId,
            Command: command,
            CapturedEvents: capturedEvents.AsReadOnly(),
            Traces: traces.AsReadOnly(),
            StateSnapshot: snapshot,
            Success: true,
            CompletedAt: DateTimeOffset.UtcNow);
    }
}
