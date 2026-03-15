using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T1M.WSS.Governance;
using Whycespace.Engines.T1M.WSS.Governance.Commands;
using Whycespace.System.Midstream.WSS.Governance;

namespace Whycespace.GovernanceWorkflow.Tests;

public class GovernanceWorkflowEngineTests
{
    private readonly Engines.T1M.WSS.Governance.GovernanceWorkflowEngine _engine = new();

    // --- Typed command tests ---

    [Fact]
    public void ExecuteStart_ValidCommand_ReturnsSuccess()
    {
        var command = new StartGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteStart(command);

        Assert.True(result.Success);
        Assert.Equal("ProposalCreated", result.CurrentStep);
        Assert.Equal("ProposalSubmitted", result.NextStep);
        Assert.Equal("Governance workflow started", result.Message);
    }

    [Fact]
    public void ExecuteStart_EmptyProposalId_ReturnsFail()
    {
        var command = new StartGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteStart(command);

        Assert.False(result.Success);
        Assert.Contains("ProposalId is required", result.Message);
    }

    [Fact]
    public void ExecuteStart_EmptyGuardianId_ReturnsFail()
    {
        var command = new StartGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);

        var result = _engine.ExecuteStart(command);

        Assert.False(result.Success);
        Assert.Contains("StartedByGuardianId is required", result.Message);
    }

    [Fact]
    public void ExecuteAdvance_ValidTransition_ReturnsSuccess()
    {
        var proposalId = Guid.NewGuid();
        var command = new AdvanceGovernanceWorkflowCommand(
            Guid.NewGuid(), proposalId, "ProposalCreated", "ProposalSubmitted",
            Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteAdvance(command);

        Assert.True(result.Success);
        Assert.Equal("ProposalSubmitted", result.CurrentStep);
        Assert.Equal("ProposalUnderReview", result.NextStep);
    }

    [Fact]
    public void ExecuteAdvance_FullLifecycle_AllTransitionsSucceed()
    {
        var proposalId = Guid.NewGuid();
        var triggeredBy = Guid.NewGuid();
        var steps = new[]
        {
            ("ProposalCreated", "ProposalSubmitted"),
            ("ProposalSubmitted", "ProposalUnderReview"),
            ("ProposalUnderReview", "VotingOpen"),
            ("VotingOpen", "VotingClosed"),
            ("VotingClosed", "QuorumEvaluation"),
            ("QuorumEvaluation", "GovernanceDecision"),
            ("GovernanceDecision", "GovernanceExecution"),
            ("GovernanceExecution", "WorkflowCompleted")
        };

        foreach (var (current, next) in steps)
        {
            var command = new AdvanceGovernanceWorkflowCommand(
                Guid.NewGuid(), proposalId, current, next, triggeredBy, DateTime.UtcNow);

            var result = _engine.ExecuteAdvance(command);
            Assert.True(result.Success, $"Transition {current} -> {next} failed: {result.Message}");
            Assert.Equal(next, result.CurrentStep);
        }
    }

    [Fact]
    public void ExecuteAdvance_InvalidTransition_ReturnsFail()
    {
        var command = new AdvanceGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), "ProposalCreated", "VotingOpen",
            Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteAdvance(command);

        Assert.False(result.Success);
        Assert.Contains("Invalid transition", result.Message);
    }

    [Fact]
    public void ExecuteAdvance_CompletedWorkflow_ReturnsFail()
    {
        var command = new AdvanceGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), "WorkflowCompleted", "ProposalCreated",
            Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteAdvance(command);

        Assert.False(result.Success);
        Assert.Contains("already completed", result.Message);
    }

    [Fact]
    public void ExecuteAdvance_InvalidStepName_ReturnsFail()
    {
        var command = new AdvanceGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), "InvalidStep", "ProposalSubmitted",
            Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteAdvance(command);

        Assert.False(result.Success);
        Assert.Contains("Invalid current step", result.Message);
    }

    [Fact]
    public void ExecuteComplete_ValidCommand_ReturnsSuccess()
    {
        var proposalId = Guid.NewGuid();
        var command = new CompleteGovernanceWorkflowCommand(
            Guid.NewGuid(), proposalId, Guid.NewGuid(), DateTime.UtcNow);

        var result = _engine.ExecuteComplete(command);

        Assert.True(result.Success);
        Assert.Equal("WorkflowCompleted", result.CurrentStep);
        Assert.Equal("", result.NextStep);
        Assert.Equal("Governance workflow completed", result.Message);
    }

    [Fact]
    public void ExecuteComplete_EmptyCompletedBy_ReturnsFail()
    {
        var command = new CompleteGovernanceWorkflowCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);

        var result = _engine.ExecuteComplete(command);

        Assert.False(result.Success);
        Assert.Contains("CompletedBy is required", result.Message);
    }

    // --- IEngine interface tests ---

    [Fact]
    public async Task ExecuteAsync_Start_ReturnsOk()
    {
        var proposalId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), "governance-wf", "start", PartitionKey.Empty,
            new Dictionary<string, object>
            {
                ["action"] = "start",
                ["proposalId"] = proposalId.ToString(),
                ["startedByGuardianId"] = guardianId.ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal(nameof(GovernanceWorkflowStartedEvent), result.Events[0].EventType);
        Assert.Equal("ProposalCreated", result.Output["currentStep"]);
    }

    [Fact]
    public async Task ExecuteAsync_Advance_ReturnsOk()
    {
        var proposalId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), "governance-wf", "advance", PartitionKey.Empty,
            new Dictionary<string, object>
            {
                ["action"] = "advance",
                ["proposalId"] = proposalId.ToString(),
                ["currentStep"] = "ProposalCreated",
                ["nextStep"] = "ProposalSubmitted",
                ["triggeredBy"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal(nameof(GovernanceWorkflowAdvancedEvent), result.Events[0].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_Complete_ReturnsOk()
    {
        var proposalId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), "governance-wf", "complete", PartitionKey.Empty,
            new Dictionary<string, object>
            {
                ["action"] = "complete",
                ["proposalId"] = proposalId.ToString(),
                ["completedBy"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal(nameof(GovernanceWorkflowCompletedEvent), result.Events[0].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ReturnsFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), "governance-wf", "unknown", PartitionKey.Empty,
            new Dictionary<string, object> { ["action"] = "invalid" });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Architecture tests ---

    [Fact]
    public void Engine_IsStateless_NoPersistenceFields()
    {
        var type = typeof(Engines.T1M.WSS.Governance.GovernanceWorkflowEngine);
        var instanceFields = type.GetFields(
            global::System.Reflection.BindingFlags.Instance |
            global::System.Reflection.BindingFlags.NonPublic |
            global::System.Reflection.BindingFlags.Public);

        Assert.Empty(instanceFields);
    }

    [Fact]
    public void Engine_ImplementsIEngine()
    {
        Assert.IsAssignableFrom<IEngine>(new Engines.T1M.WSS.Governance.GovernanceWorkflowEngine());
    }

    [Fact]
    public void Engine_Name_IsCorrect()
    {
        Assert.Equal("GovernanceWorkflowEngine", _engine.Name);
    }

    // --- Concurrency tests ---

    [Fact]
    public async Task ConcurrentAdvances_AllReturnDeterministically()
    {
        var proposalId = Guid.NewGuid();
        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var command = new AdvanceGovernanceWorkflowCommand(
                Guid.NewGuid(), proposalId, "ProposalCreated", "ProposalSubmitted",
                Guid.NewGuid(), DateTime.UtcNow);
            return Task.Run(() => _engine.ExecuteAdvance(command));
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Equal("ProposalSubmitted", r.CurrentStep);
        });
    }
}
