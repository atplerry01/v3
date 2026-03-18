namespace Whycespace.WorkflowRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;
using Whycespace.WorkflowRuntime.Dispatcher;
using Xunit;

public sealed class WorkflowRuntimeDispatcherTests
{
    private readonly EngineInvocationRegistry _registry = new();
    private readonly FakeEventPublisher _eventPublisher = new();

    private WorkflowRuntimeDispatcher CreateDispatcher(IWorkflowPolicyEvaluator? policyEvaluator = null)
    {
        return new WorkflowRuntimeDispatcher(
            _registry,
            policyEvaluator ?? new AllowAllPolicyEvaluator(),
            _eventPublisher);
    }

    private static WorkflowDispatchCommand CreateCommand(string engineName = "TestEngine")
    {
        return WorkflowDispatchCommand.Create(
            Guid.NewGuid(),
            "step-1",
            engineName,
            new Dictionary<string, object> { ["action"] = "test" },
            Guid.NewGuid().ToString(),
            "test-user");
    }

    [Fact]
    public async Task SuccessfulEngineInvocation_ReturnsExecutedResult()
    {
        var engine = new FakeEngine("TestEngine", EngineResult.Ok(
            new[] { EngineEvent.Create("TestEvent", Guid.NewGuid()) }));
        _registry.Register(engine);

        var dispatcher = CreateDispatcher();
        var command = CreateCommand();

        var result = await dispatcher.DispatchAsync(command);

        Assert.Equal(ExecutionStatus.Executed, result.ExecutionStatus);
        Assert.Equal(command.WorkflowInstanceId, result.WorkflowInstanceId);
        Assert.Equal("TestEngine", result.EngineName);
        Assert.NotNull(result.EvidenceHash);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task PolicyRejection_ReturnsBlockedByPolicy()
    {
        var engine = new FakeEngine("TestEngine", EngineResult.Ok(Array.Empty<EngineEvent>()));
        _registry.Register(engine);

        var dispatcher = CreateDispatcher(new DenyAllPolicyEvaluator());
        var command = CreateCommand();

        var result = await dispatcher.DispatchAsync(command);

        Assert.Equal(ExecutionStatus.BlockedByPolicy, result.ExecutionStatus);
        Assert.Null(result.EvidenceHash);
        Assert.Null(result.EventId);
    }

    [Fact]
    public async Task UnregisteredEngine_ReturnsFailed()
    {
        var dispatcher = CreateDispatcher();
        var command = CreateCommand("NonExistentEngine");

        var result = await dispatcher.DispatchAsync(command);

        Assert.Equal(ExecutionStatus.Failed, result.ExecutionStatus);
    }

    [Fact]
    public async Task EventEmittedAfterExecution()
    {
        var aggregateId = Guid.NewGuid();
        var engine = new FakeEngine("TestEngine", EngineResult.Ok(
            new[] { EngineEvent.Create("OrderPlaced", aggregateId) }));
        _registry.Register(engine);

        var dispatcher = CreateDispatcher();
        var command = CreateCommand();

        await dispatcher.DispatchAsync(command);

        Assert.Single(_eventPublisher.Published);
        var published = _eventPublisher.Published[0];
        Assert.Equal("OrderPlaced", published.envelope.EventType);
        Assert.Equal(aggregateId.ToString(), published.envelope.AggregateId);
    }

    [Fact]
    public async Task EvidenceHashGeneration_IsDeterministic()
    {
        var engine = new FakeEngine("TestEngine", EngineResult.Ok(
            new[] { EngineEvent.Create("TestEvent", Guid.NewGuid()) }));
        _registry.Register(engine);

        var command = CreateCommand();
        var result = EngineResult.Ok(new[] { EngineEvent.Create("TestEvent", Guid.NewGuid()) });
        var context = EngineInvocationContext.Create(command, Guid.NewGuid(), "1.0.0");

        var hash1 = WorkflowRuntimeDispatcher.ComputeEvidenceHash(command, result, context);
        var hash2 = WorkflowRuntimeDispatcher.ComputeEvidenceHash(command, result, context);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA256 hex length
    }

    [Fact]
    public async Task ConcurrentDispatchSafety()
    {
        var engine = new FakeEngine("TestEngine", EngineResult.Ok(
            new[] { EngineEvent.Create("TestEvent", Guid.NewGuid()) }));
        _registry.Register(engine);

        var dispatcher = CreateDispatcher();
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => dispatcher.DispatchAsync(CreateCommand()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(ExecutionStatus.Executed, r.ExecutionStatus));
        var invocationIds = results.Select(r => r.InvocationId).ToHashSet();
        Assert.Equal(10, invocationIds.Count); // All unique
    }

    [Fact]
    public async Task EngineFails_ReturnsFailed()
    {
        var engine = new FakeEngine("TestEngine", EngineResult.Fail("Engine error"));
        _registry.Register(engine);

        var dispatcher = CreateDispatcher();
        var command = CreateCommand();

        var result = await dispatcher.DispatchAsync(command);

        Assert.Equal(ExecutionStatus.Failed, result.ExecutionStatus);
        Assert.Empty(_eventPublisher.Published);
    }

    // --- Test doubles ---

    private sealed class FakeEngine : IEngine
    {
        private readonly EngineResult _result;
        public string Name { get; }

        public FakeEngine(string name, EngineResult result)
        {
            Name = name;
            _result = result;
        }

        public Task<EngineResult> ExecuteAsync(EngineContext context) =>
            Task.FromResult(_result);
    }

    private sealed class AllowAllPolicyEvaluator : IWorkflowPolicyEvaluator
    {
        public Task<PolicyEvaluationOutcome> EvaluateAsync(
            EngineInvocationContext context, CancellationToken cancellationToken)
            => Task.FromResult(PolicyEvaluationOutcome.Allow());
    }

    private sealed class DenyAllPolicyEvaluator : IWorkflowPolicyEvaluator
    {
        public Task<PolicyEvaluationOutcome> EvaluateAsync(
            EngineInvocationContext context, CancellationToken cancellationToken)
            => Task.FromResult(PolicyEvaluationOutcome.Deny("Access denied by policy"));
    }

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public List<(string topic, EventEnvelope envelope)> Published { get; } = new();

        public Task PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken)
        {
            Published.Add((topic, envelope));
            return Task.CompletedTask;
        }
    }
}
