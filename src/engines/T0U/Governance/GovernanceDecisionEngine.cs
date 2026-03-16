namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Domain.Events.Governance;

public sealed class GovernanceDecisionEngine
{
    private static readonly decimal SimpleMajorityThreshold = 50.0m;
    private static readonly decimal SuperMajorityThreshold = 66.67m;
    private static readonly decimal ConstitutionalMajorityThreshold = 75.0m;
    private static readonly decimal EscalationMargin = 5.0m;

    public (GovernanceDecisionResult Result, GovernanceDecisionEvaluatedEvent? EvaluatedEvent, object? OutcomeEvent) Execute(EvaluateGovernanceDecisionCommand command)
    {
        if (!Enum.IsDefined(command.DecisionRule))
            return (Failure(command, "Invalid decision rule."), null, null);

        if (command.VotesApprove < 0 || command.VotesReject < 0 || command.VotesAbstain < 0)
            return (Failure(command, "Vote counts must not be negative."), null, null);

        var totalVotes = command.VotesApprove + command.VotesReject + command.VotesAbstain;
        if (totalVotes == 0)
            return (Failure(command, "No votes cast."), null, null);

        var evaluatedAt = DateTime.UtcNow;

        var evaluatedEvent = new GovernanceDecisionEvaluatedEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            ApprovalPercentage: command.ApprovalPercentage,
            ParticipationPercentage: command.ParticipationPercentage,
            DecisionRule: command.DecisionRule.ToString(),
            EvaluatedAt: evaluatedAt);

        if (!command.QuorumMet)
        {
            var rejectedResult = new GovernanceDecisionResult(
                Success: true,
                ProposalId: command.ProposalId,
                DecisionOutcome: DecisionOutcome.Rejected,
                ApprovalPercentage: command.ApprovalPercentage,
                ParticipationPercentage: command.ParticipationPercentage,
                DecisionRule: command.DecisionRule,
                Message: "Decision rejected: quorum not met.",
                ExecutedAt: evaluatedAt);

            var rejectedEvent = new GovernanceDecisionRejectedEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                DecisionRule: command.DecisionRule.ToString(),
                ApprovalPercentage: command.ApprovalPercentage,
                RejectedAt: evaluatedAt);

            return (rejectedResult, evaluatedEvent, rejectedEvent);
        }

        var outcome = EvaluateOutcome(command);

        var message = outcome switch
        {
            DecisionOutcome.Approved => $"Decision approved under {command.DecisionRule} with {command.ApprovalPercentage}% approval.",
            DecisionOutcome.Rejected => $"Decision rejected under {command.DecisionRule} with {command.ApprovalPercentage}% approval.",
            DecisionOutcome.Escalated => $"Decision escalated: approval {command.ApprovalPercentage}% within escalation margin for {command.DecisionRule}.",
            _ => "Decision evaluated."
        };

        var result = new GovernanceDecisionResult(
            Success: true,
            ProposalId: command.ProposalId,
            DecisionOutcome: outcome,
            ApprovalPercentage: command.ApprovalPercentage,
            ParticipationPercentage: command.ParticipationPercentage,
            DecisionRule: command.DecisionRule,
            Message: message,
            ExecutedAt: evaluatedAt);

        object? outcomeEvent = outcome switch
        {
            DecisionOutcome.Approved => new GovernanceDecisionApprovedEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                DecisionRule: command.DecisionRule.ToString(),
                ApprovalPercentage: command.ApprovalPercentage,
                ApprovedAt: evaluatedAt),

            DecisionOutcome.Rejected => new GovernanceDecisionRejectedEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                DecisionRule: command.DecisionRule.ToString(),
                ApprovalPercentage: command.ApprovalPercentage,
                RejectedAt: evaluatedAt),

            DecisionOutcome.Escalated => new GovernanceDecisionEscalatedEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                EscalationReason: $"Approval {command.ApprovalPercentage}% within escalation margin for {command.DecisionRule}.",
                EscalatedAt: evaluatedAt),

            _ => null
        };

        return (result, evaluatedEvent, outcomeEvent);
    }

    private static DecisionOutcome EvaluateOutcome(EvaluateGovernanceDecisionCommand command)
    {
        if (command.DecisionRule == DecisionRule.EmergencyOverride)
            return DecisionOutcome.Approved;

        var threshold = GetThreshold(command.DecisionRule);

        if (command.ApprovalPercentage >= threshold)
            return DecisionOutcome.Approved;

        if (command.ApprovalPercentage >= threshold - EscalationMargin)
            return DecisionOutcome.Escalated;

        return DecisionOutcome.Rejected;
    }

    private static decimal GetThreshold(DecisionRule rule) => rule switch
    {
        DecisionRule.SimpleMajority => SimpleMajorityThreshold,
        DecisionRule.SuperMajority => SuperMajorityThreshold,
        DecisionRule.ConstitutionalMajority => ConstitutionalMajorityThreshold,
        _ => SimpleMajorityThreshold
    };

    private static GovernanceDecisionResult Failure(EvaluateGovernanceDecisionCommand command, string message)
    {
        return new GovernanceDecisionResult(
            Success: false,
            ProposalId: command.ProposalId,
            DecisionOutcome: DecisionOutcome.Rejected,
            ApprovalPercentage: command.ApprovalPercentage,
            ParticipationPercentage: command.ParticipationPercentage,
            DecisionRule: command.DecisionRule,
            Message: message,
            ExecutedAt: DateTime.UtcNow);
    }
}
