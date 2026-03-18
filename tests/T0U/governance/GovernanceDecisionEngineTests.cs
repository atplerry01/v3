using Whycespace.Engines.T0U.Governance.Proposal.Validation;
using Whycespace.Engines.T0U.Governance.Proposal.Lifecycle;
using Whycespace.Engines.T0U.Governance.Voting.Casting;
using Whycespace.Engines.T0U.Governance.Quorum.Evaluation;
using Whycespace.Engines.T0U.Governance.Delegation.Assignment;
using Whycespace.Engines.T0U.Governance.Dispute.Raising;
using Whycespace.Engines.T0U.Governance.Emergency.Trigger;
using Whycespace.Engines.T0U.Governance.Roles.Assignment;
using Whycespace.Engines.T0U.Governance.Domain.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Validation;
using Whycespace.Engines.T0U.Governance.Evidence.Recording;
using Whycespace.Engines.T0U.Governance.Evidence.Audit;
using Whycespace.Engines.T0U.Governance.Workflow.Execution;
using Whycespace.Engines.T0U.Governance.Decisions.Evaluation;
using Whycespace.Engines.T0U.Governance.Guardians.Registry;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Domain.Events.Governance;

namespace Whycespace.Governance.Tests;

public class GovernanceDecisionEngineTests
{
    private readonly GovernanceDecisionEngine _engine = new();

    private static EvaluateGovernanceDecisionCommand MakeCommand(
        bool quorumMet = true,
        int approve = 7,
        int reject = 3,
        int abstain = 0,
        decimal approvalPct = 70m,
        decimal participationPct = 100m,
        DecisionRule rule = DecisionRule.SimpleMajority)
    {
        return new EvaluateGovernanceDecisionCommand(
            Guid.NewGuid(), "p-cmd", quorumMet,
            approve, reject, abstain,
            approvalPct, participationPct, rule, DateTime.UtcNow);
    }

    [Fact]
    public void Execute_SimpleMajority_Approved()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(approvalPct: 60m));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Approved, result.DecisionOutcome);
        Assert.NotNull(evaluated);
        Assert.IsType<GovernanceDecisionApprovedEvent>(outcome);
    }

    [Fact]
    public void Execute_SimpleMajority_Rejected()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(approvalPct: 40m));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Rejected, result.DecisionOutcome);
        Assert.NotNull(evaluated);
        Assert.IsType<GovernanceDecisionRejectedEvent>(outcome);
    }

    [Fact]
    public void Execute_SimpleMajority_Escalated()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(approvalPct: 47m));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Escalated, result.DecisionOutcome);
        Assert.NotNull(evaluated);
        Assert.IsType<GovernanceDecisionEscalatedEvent>(outcome);
    }

    [Fact]
    public void Execute_SuperMajority_Approved()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 70m, rule: DecisionRule.SuperMajority));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Approved, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_SuperMajority_Rejected()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 55m, rule: DecisionRule.SuperMajority));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Rejected, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_SuperMajority_Escalated()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 63m, rule: DecisionRule.SuperMajority));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Escalated, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_ConstitutionalMajority_Approved()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 80m, rule: DecisionRule.ConstitutionalMajority));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Approved, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_ConstitutionalMajority_Rejected()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 60m, rule: DecisionRule.ConstitutionalMajority));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Rejected, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_EmergencyOverride_AlwaysApproved()
    {
        var (result, _, _) = _engine.Execute(MakeCommand(approvalPct: 10m, rule: DecisionRule.EmergencyOverride));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Approved, result.DecisionOutcome);
    }

    [Fact]
    public void Execute_QuorumNotMet_Rejected()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(quorumMet: false, approvalPct: 90m));

        Assert.True(result.Success);
        Assert.Equal(DecisionOutcome.Rejected, result.DecisionOutcome);
        Assert.Contains("quorum not met", result.Message);
        Assert.NotNull(evaluated);
        Assert.IsType<GovernanceDecisionRejectedEvent>(outcome);
    }

    [Fact]
    public void Execute_NegativeVotes_Failure()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(approve: -1));

        Assert.False(result.Success);
        Assert.Contains("negative", result.Message);
        Assert.Null(evaluated);
        Assert.Null(outcome);
    }

    [Fact]
    public void Execute_ZeroVotes_Failure()
    {
        var (result, evaluated, outcome) = _engine.Execute(MakeCommand(approve: 0, reject: 0, abstain: 0));

        Assert.False(result.Success);
        Assert.Contains("No votes", result.Message);
        Assert.Null(evaluated);
        Assert.Null(outcome);
    }

    [Fact]
    public void Execute_InvalidDecisionRule_Failure()
    {
        var command = new EvaluateGovernanceDecisionCommand(
            Guid.NewGuid(), "p-invalid", true,
            5, 3, 0, 62.5m, 100m, (DecisionRule)999, DateTime.UtcNow);

        var (result, evaluated, outcome) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Invalid decision rule", result.Message);
        Assert.Null(evaluated);
        Assert.Null(outcome);
    }

    [Fact]
    public void Execute_ConcurrentCalls_Deterministic()
    {
        var command = MakeCommand(approvalPct: 70m, rule: DecisionRule.SuperMajority);
        var results = new GovernanceDecisionResult[100];

        Parallel.For(0, 100, i =>
        {
            var (result, _, _) = _engine.Execute(command);
            results[i] = result;
        });

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Equal(DecisionOutcome.Approved, r.DecisionOutcome);
        });
    }

    [Fact]
    public void Execute_Engine_IsStateless()
    {
        var cmd1 = MakeCommand(approvalPct: 80m, rule: DecisionRule.ConstitutionalMajority);
        var cmd2 = MakeCommand(approvalPct: 40m, rule: DecisionRule.SimpleMajority);

        var (r1, _, _) = _engine.Execute(cmd1);
        var (r2, _, _) = _engine.Execute(cmd2);

        Assert.Equal(DecisionOutcome.Approved, r1.DecisionOutcome);
        Assert.Equal(DecisionOutcome.Rejected, r2.DecisionOutcome);
    }

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(GovernanceDecisionEngine).IsSealed);
    }

    [Fact]
    public void Execute_EvaluatedEvent_HasCorrectFields()
    {
        var command = MakeCommand(approvalPct: 70m, participationPct: 85m, rule: DecisionRule.SuperMajority);

        var (_, evaluated, _) = _engine.Execute(command);

        Assert.NotNull(evaluated);
        Assert.Equal(command.ProposalId, evaluated!.ProposalId);
        Assert.Equal(70m, evaluated.ApprovalPercentage);
        Assert.Equal(85m, evaluated.ParticipationPercentage);
        Assert.Equal("SuperMajority", evaluated.DecisionRule);
        Assert.NotEqual(Guid.Empty, evaluated.EventId);
    }

    [Fact]
    public void Execute_ApprovedEvent_HasCorrectFields()
    {
        var command = MakeCommand(approvalPct: 80m, rule: DecisionRule.ConstitutionalMajority);

        var (_, _, outcome) = _engine.Execute(command);

        var approved = Assert.IsType<GovernanceDecisionApprovedEvent>(outcome);
        Assert.Equal(command.ProposalId, approved.ProposalId);
        Assert.Equal("ConstitutionalMajority", approved.DecisionRule);
        Assert.Equal(80m, approved.ApprovalPercentage);
    }

    [Fact]
    public void Execute_EscalatedEvent_HasEscalationReason()
    {
        var (_, _, outcome) = _engine.Execute(MakeCommand(approvalPct: 63m, rule: DecisionRule.SuperMajority));

        var escalated = Assert.IsType<GovernanceDecisionEscalatedEvent>(outcome);
        Assert.Contains("escalation margin", escalated.EscalationReason);
        Assert.Equal("p-cmd", escalated.ProposalId);
    }

    [Fact]
    public void Execute_SameInput_SameOutcome()
    {
        var command = MakeCommand(approvalPct: 65m, rule: DecisionRule.SuperMajority);

        var (r1, _, _) = _engine.Execute(command);
        var (r2, _, _) = _engine.Execute(command);

        Assert.Equal(r1.DecisionOutcome, r2.DecisionOutcome);
        Assert.Equal(r1.Success, r2.Success);
        Assert.Equal(r1.Message, r2.Message);
    }

    [Fact]
    public void Execute_NoConstructorDependencies()
    {
        var engine = new GovernanceDecisionEngine();
        var (result, evaluated, _) = engine.Execute(MakeCommand());

        Assert.True(result.Success);
        Assert.NotNull(evaluated);
    }
}
