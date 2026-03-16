using Whycespace.Engines.T0U.Governance;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceWorkflowEngineTests
{
    private readonly GovernanceWorkflowStore _workflowStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceWorkflowEngine _engine;

    public GovernanceWorkflowEngineTests()
    {
        _engine = new GovernanceWorkflowEngine(_workflowStore, _proposalStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());

        var registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        registryEngine.CreateProposal("p-1", "Proposal", "Desc", ProposalType.Policy, "g-alice");
    }

    [Fact]
    public void StartWorkflow_Succeeds()
    {
        var workflow = _engine.StartWorkflow("w-1", "p-1");

        Assert.Equal("w-1", workflow.WorkflowId);
        Assert.Equal("p-1", workflow.ProposalId);
        Assert.Equal(WorkflowStage.Create, workflow.Stage);
        Assert.Null(workflow.CompletedAt);
    }

    [Fact]
    public void StartWorkflow_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.StartWorkflow("w-bad", "nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void StartWorkflow_Duplicate_Throws()
    {
        _engine.StartWorkflow("w-dup", "p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.StartWorkflow("w-dup", "p-1"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void AdvanceWorkflow_FullLifecycle()
    {
        _engine.StartWorkflow("w-lc", "p-1");

        var review = _engine.AdvanceWorkflow("w-lc");
        Assert.Equal(WorkflowStage.Review, review.Stage);

        var voting = _engine.AdvanceWorkflow("w-lc");
        Assert.Equal(WorkflowStage.Voting, voting.Stage);

        var decision = _engine.AdvanceWorkflow("w-lc");
        Assert.Equal(WorkflowStage.Decision, decision.Stage);

        var execution = _engine.AdvanceWorkflow("w-lc");
        Assert.Equal(WorkflowStage.Execution, execution.Stage);
    }

    [Fact]
    public void AdvanceWorkflow_FromExecution_Throws()
    {
        _engine.StartWorkflow("w-exec", "p-1");
        _engine.AdvanceWorkflow("w-exec"); // Review
        _engine.AdvanceWorkflow("w-exec"); // Voting
        _engine.AdvanceWorkflow("w-exec"); // Decision
        _engine.AdvanceWorkflow("w-exec"); // Execution

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.AdvanceWorkflow("w-exec"));
        Assert.Contains("Use CompleteWorkflow", ex.Message);
    }

    [Fact]
    public void AdvanceWorkflow_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.AdvanceWorkflow("nonexistent"));
        Assert.Contains("Workflow not found", ex.Message);
    }

    [Fact]
    public void CompleteWorkflow_Succeeds()
    {
        _engine.StartWorkflow("w-comp", "p-1");
        _engine.AdvanceWorkflow("w-comp"); // Review
        _engine.AdvanceWorkflow("w-comp"); // Voting
        _engine.AdvanceWorkflow("w-comp"); // Decision
        _engine.AdvanceWorkflow("w-comp"); // Execution

        var completed = _engine.CompleteWorkflow("w-comp");

        Assert.Equal(WorkflowStage.Completed, completed.Stage);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public void CompleteWorkflow_NotExecution_Throws()
    {
        _engine.StartWorkflow("w-early", "p-1");
        _engine.AdvanceWorkflow("w-early"); // Review

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CompleteWorkflow("w-early"));
        Assert.Contains("must be in Execution stage", ex.Message);
    }

    [Fact]
    public void CompleteWorkflow_AlreadyCompleted_Throws()
    {
        _engine.StartWorkflow("w-done", "p-1");
        _engine.AdvanceWorkflow("w-done"); // Review
        _engine.AdvanceWorkflow("w-done"); // Voting
        _engine.AdvanceWorkflow("w-done"); // Decision
        _engine.AdvanceWorkflow("w-done"); // Execution
        _engine.CompleteWorkflow("w-done");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CompleteWorkflow("w-done"));
        Assert.Contains("already completed", ex.Message);
    }
}
