namespace Whycespace.WorkflowRuntime.Dispatcher;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;
using Whycespace.EventFabric.Topics;

public sealed class WorkflowRuntimeDispatcher
{
    private readonly EngineInvocationRegistry _registry;
    private readonly IWorkflowPolicyEvaluator _policyEvaluator;
    private readonly IEventPublisher _eventPublisher;

    public WorkflowRuntimeDispatcher(
        EngineInvocationRegistry registry,
        IWorkflowPolicyEvaluator policyEvaluator,
        IEventPublisher eventPublisher)
    {
        _registry = registry;
        _policyEvaluator = policyEvaluator;
        _eventPublisher = eventPublisher;
    }

    public async Task<WorkflowDispatchResult> DispatchAsync(
        WorkflowDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var invocationId = Guid.NewGuid();

        // Step 1: Resolve engine
        var registration = _registry.Resolve(command.EngineName);
        if (registration is null)
        {
            return WorkflowDispatchResult.Failed(
                invocationId, command.WorkflowInstanceId, command.EngineName);
        }

        // Step 2: Build invocation context
        var invocationContext = EngineInvocationContext.Create(
            command, invocationId, registration.Version);

        // Step 3: Evaluate policy
        var policyOutcome = await _policyEvaluator.EvaluateAsync(
            invocationContext, cancellationToken);

        if (!policyOutcome.Allowed)
        {
            return WorkflowDispatchResult.BlockedByPolicy(
                invocationId, command.WorkflowInstanceId, command.EngineName);
        }

        // Step 4: Invoke engine
        var engineContext = new EngineContext(
            invocationId,
            command.WorkflowInstanceId.ToString(),
            command.StepId,
            new PartitionKey(command.WorkflowInstanceId.ToString()),
            command.EngineCommandPayload);

        var result = await registration.Engine.ExecuteAsync(engineContext);

        if (!result.Success)
        {
            return WorkflowDispatchResult.Failed(
                invocationId, command.WorkflowInstanceId, command.EngineName);
        }

        // Step 5: Emit events to Kafka
        var eventId = Guid.NewGuid();
        foreach (var evt in result.Events)
        {
            await _eventPublisher.PublishAsync(
                EventTopics.EngineEvents,
                new EventEnvelope(
                    evt.EventId,
                    evt.EventType,
                    EventTopics.EngineEvents,
                    evt,
                    new PartitionKey(evt.AggregateId.ToString()),
                    Timestamp.Now(),
                    AggregateId: evt.AggregateId.ToString(),
                    Metadata: new Dictionary<string, string>
                    {
                        ["WorkflowInstanceId"] = command.WorkflowInstanceId.ToString(),
                        ["InvocationId"] = invocationId.ToString(),
                        ["CorrelationId"] = command.CorrelationId
                    }),
                cancellationToken);

            eventId = evt.EventId;
        }

        // Step 6: Compute evidence hash
        var evidenceHash = ComputeEvidenceHash(command, result, invocationContext);

        return WorkflowDispatchResult.Executed(
            invocationId, command.WorkflowInstanceId, command.EngineName, eventId, evidenceHash);
    }

    public static string ComputeEvidenceHash(
        WorkflowDispatchCommand command,
        EngineResult result,
        EngineInvocationContext context)
    {
        var payload = new
        {
            Command = new
            {
                command.WorkflowInstanceId,
                command.StepId,
                command.EngineName,
                command.CorrelationId,
                command.RequestedBy
            },
            Result = new
            {
                result.Success,
                EventCount = result.Events.Count
            },
            Context = new
            {
                context.InvocationId,
                context.WorkflowInstanceId,
                context.EngineName,
                context.EngineVersion,
                context.Timestamp
            }
        };

        var json = JsonSerializer.Serialize(payload, EvidenceJsonOptions);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
