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
using Whycespace.Engines.T0U.Governance.Proposal.Creation;
using Whycespace.Engines.T0U.Governance.Proposal.Submission;
using Whycespace.Engines.T0U.Governance.Proposal.Cancellation;
using Whycespace.Engines.T0U.Governance.Voting.Validation;
using Whycespace.Engines.T0U.Governance.Voting.Withdrawal;
using Whycespace.Engines.T0U.Governance.Delegation.Revocation;
using Whycespace.Engines.T0U.Governance.Dispute.Resolution;
using Whycespace.Engines.T0U.Governance.Dispute.Withdrawal;
using Whycespace.Engines.T0U.Governance.Emergency.Validation;
using Whycespace.Engines.T0U.Governance.Emergency.Revocation;
using Whycespace.Engines.T0U.Governance.Roles.Revocation;
using Whycespace.Engines.T0U.Governance.Domain.Validation;
using Whycespace.Engines.T0U.Governance.Domain.Deactivation;
using Whycespace.Engines.T0U.Governance.ProposalType.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Deactivation;
using Whycespace.Domain.Events.Governance;

namespace Whycespace.Governance.Tests;

public class QuorumEngineTests
{
    private static EvaluateQuorumCommand MakeCommand(
        int totalEligible = 10,
        int votesCast = 6,
        int votesApprove = 4,
        int votesReject = 1,
        int votesAbstain = 1,
        decimal requiredParticipation = 50m,
        decimal requiredApproval = 50m)
    {
        return new EvaluateQuorumCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: Guid.NewGuid(),
            TotalEligibleGuardians: totalEligible,
            VotesCast: votesCast,
            VotesApprove: votesApprove,
            VotesReject: votesReject,
            VotesAbstain: votesAbstain,
            RequiredParticipationPercentage: requiredParticipation,
            RequiredApprovalPercentage: requiredApproval,
            Timestamp: DateTime.UtcNow);
    }

    private static QuorumEngine CreateEngine() => new();

    [Fact]
    public void Execute_SimpleMajority_QuorumMet()
    {
        var engine = CreateEngine();
        // 6/10 = 60% participation (>= 50%), 4/6 = 66.67% approval (>= 50%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 6,
            votesApprove: 4, votesReject: 1, votesAbstain: 1,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, evaluatedEvent, outcomeEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.True(result.QuorumMet);
        Assert.Equal(60m, result.ParticipationPercentage);
        Assert.Equal("Quorum met", result.Message);
        Assert.NotNull(evaluatedEvent);
        Assert.IsType<GovernanceQuorumMetEvent>(outcomeEvent);
    }

    [Fact]
    public void Execute_SuperMajority_QuorumMet()
    {
        var engine = CreateEngine();
        // 8/10 = 80% participation (>= 60%), 6/8 = 75% approval (>= 66%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 8,
            votesApprove: 6, votesReject: 1, votesAbstain: 1,
            requiredParticipation: 60m, requiredApproval: 66m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.True(result.QuorumMet);
        Assert.IsType<GovernanceQuorumMetEvent>(outcomeEvent);
    }

    [Fact]
    public void Execute_ConstitutionalDecision_QuorumMet()
    {
        var engine = CreateEngine();
        // 8/10 = 80% participation (>= 75%), 7/8 = 87.5% approval (>= 75%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 8,
            votesApprove: 7, votesReject: 1, votesAbstain: 0,
            requiredParticipation: 75m, requiredApproval: 75m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.True(result.QuorumMet);
        Assert.IsType<GovernanceQuorumMetEvent>(outcomeEvent);
    }

    [Fact]
    public void Execute_EmergencyDecision_QuorumMet()
    {
        var engine = CreateEngine();
        // 5/10 = 50% participation (>= 40%), 4/5 = 80% approval (>= 80%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 5,
            votesApprove: 4, votesReject: 1, votesAbstain: 0,
            requiredParticipation: 40m, requiredApproval: 80m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.True(result.QuorumMet);
        Assert.IsType<GovernanceQuorumMetEvent>(outcomeEvent);
    }

    [Fact]
    public void Execute_ParticipationNotMet_QuorumFailed()
    {
        var engine = CreateEngine();
        // 3/10 = 30% participation (< 50%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 3,
            votesApprove: 3, votesReject: 0, votesAbstain: 0,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, evaluatedEvent, outcomeEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.False(result.QuorumMet);
        Assert.Contains("Participation", result.Message);
        Assert.NotNull(evaluatedEvent);
        Assert.IsType<GovernanceQuorumFailedEvent>(outcomeEvent);
        var failed = (GovernanceQuorumFailedEvent)outcomeEvent;
        Assert.Contains("Participation", failed.FailureReason);
    }

    [Fact]
    public void Execute_ApprovalNotMet_QuorumFailed()
    {
        var engine = CreateEngine();
        // 8/10 = 80% participation (>= 50%), 2/8 = 25% approval (< 50%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 8,
            votesApprove: 2, votesReject: 5, votesAbstain: 1,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.False(result.QuorumMet);
        Assert.IsType<GovernanceQuorumFailedEvent>(outcomeEvent);
        var failed = (GovernanceQuorumFailedEvent)outcomeEvent;
        Assert.Contains("Approval", failed.FailureReason);
    }

    [Fact]
    public void Execute_BothThresholdsNotMet_ReportsBothReasons()
    {
        var engine = CreateEngine();
        // 3/10 = 30% participation (< 50%), 1/3 = 33.33% approval (< 50%)
        var command = MakeCommand(
            totalEligible: 10, votesCast: 3,
            votesApprove: 1, votesReject: 1, votesAbstain: 1,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.False(result.QuorumMet);
        var failed = (GovernanceQuorumFailedEvent)outcomeEvent;
        Assert.Contains("Participation", failed.FailureReason);
        Assert.Contains("Approval", failed.FailureReason);
    }

    // --- Validation tests ---

    [Fact]
    public void Execute_ZeroEligibleGuardians_ReturnsFailure()
    {
        var engine = CreateEngine();
        var command = MakeCommand(totalEligible: 0);

        var (result, _, _) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("greater than zero", result.Message);
    }

    [Fact]
    public void Execute_NegativeVoteCounts_ReturnsFailure()
    {
        var engine = CreateEngine();
        var command = MakeCommand(votesApprove: -1, votesCast: 5, votesReject: 5, votesAbstain: 1);

        var (result, _, _) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("negative", result.Message);
    }

    [Fact]
    public void Execute_VotesCastExceedsEligible_ReturnsFailure()
    {
        var engine = CreateEngine();
        var command = MakeCommand(totalEligible: 5, votesCast: 6, votesApprove: 4, votesReject: 1, votesAbstain: 1);

        var (result, _, _) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("exceed", result.Message);
    }

    [Fact]
    public void Execute_VoteBreakdownMismatch_ReturnsFailure()
    {
        var engine = CreateEngine();
        var command = MakeCommand(votesCast: 6, votesApprove: 4, votesReject: 1, votesAbstain: 0);

        var (result, _, _) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("breakdown", result.Message);
    }

    // --- Event field tests ---

    [Fact]
    public void Execute_EvaluatedEvent_ContainsCorrectFields()
    {
        var engine = CreateEngine();
        var command = MakeCommand();

        var (_, evaluatedEvent, _) = engine.Execute(command);

        Assert.NotNull(evaluatedEvent);
        Assert.Equal(command.ProposalId, evaluatedEvent.ProposalId);
        Assert.Equal(command.TotalEligibleGuardians, evaluatedEvent.TotalEligibleGuardians);
        Assert.Equal(command.VotesCast, evaluatedEvent.VotesCast);
        Assert.Equal(command.VotesApprove, evaluatedEvent.VotesApprove);
        Assert.Equal(command.VotesReject, evaluatedEvent.VotesReject);
        Assert.Equal(command.VotesAbstain, evaluatedEvent.VotesAbstain);
        Assert.NotEqual(Guid.Empty, evaluatedEvent.EventId);
    }

    [Fact]
    public void Execute_CorrectPercentageCalculation()
    {
        var engine = CreateEngine();
        // 7/10 = 70% participation, 5/7 = 71.43% approval
        var command = MakeCommand(
            totalEligible: 10, votesCast: 7,
            votesApprove: 5, votesReject: 1, votesAbstain: 1,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, evaluatedEvent, _) = engine.Execute(command);

        Assert.Equal(70m, result.ParticipationPercentage);
        Assert.True(Math.Abs(71.4285714285714285714285714m - result.ApprovalPercentage) < 0.01m);
        Assert.NotNull(evaluatedEvent);
        Assert.Equal(result.ParticipationPercentage, evaluatedEvent.ParticipationPercentage);
        Assert.Equal(result.ApprovalPercentage, evaluatedEvent.ApprovalPercentage);
    }

    // --- Concurrency tests ---

    [Fact]
    public void Execute_ConcurrentEvaluations_AllDeterministic()
    {
        var engine = CreateEngine();
        var commands = Enumerable.Range(0, 100).Select(i => MakeCommand(
            totalEligible: 10, votesCast: 6,
            votesApprove: 4, votesReject: 1, votesAbstain: 1,
            requiredParticipation: 50m, requiredApproval: 50m)).ToList();

        var results = new global::System.Collections.Concurrent.ConcurrentBag<bool>();

        Parallel.ForEach(commands, cmd =>
        {
            var (result, _, _) = engine.Execute(cmd);
            results.Add(result.QuorumMet);
        });

        Assert.Equal(100, results.Count);
        Assert.All(results, met => Assert.True(met));
    }

    // --- Architecture tests ---

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(QuorumEngine).IsSealed);
    }

    [Fact]
    public void Engine_HasParameterlessConstructor()
    {
        var constructors = typeof(QuorumEngine).GetConstructors();
        Assert.Single(constructors);
        Assert.Empty(constructors[0].GetParameters());
    }

    [Fact]
    public void Execute_NoPersistenceLogic_ReturnsResult()
    {
        var engine = CreateEngine();
        var command = MakeCommand();

        var (result, evaluatedEvent, outcomeEvent) = engine.Execute(command);

        // Engine returns result and events — it does not persist anything
        Assert.NotNull(result);
        Assert.NotNull(evaluatedEvent);
        Assert.NotNull(outcomeEvent);
    }

    [Fact]
    public void Execute_ZeroVotesCast_ZeroApprovalPercentage()
    {
        var engine = CreateEngine();
        var command = MakeCommand(
            totalEligible: 10, votesCast: 0,
            votesApprove: 0, votesReject: 0, votesAbstain: 0,
            requiredParticipation: 50m, requiredApproval: 50m);

        var (result, _, outcomeEvent) = engine.Execute(command);

        Assert.Equal(0m, result.ParticipationPercentage);
        Assert.Equal(0m, result.ApprovalPercentage);
        Assert.False(result.QuorumMet);
        Assert.IsType<GovernanceQuorumFailedEvent>(outcomeEvent);
    }
}
