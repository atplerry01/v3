namespace Whycespace.Engines.T0U.WhyceGovernance;

using Whycespace.Engines.T0U.WhyceGovernance.Commands;
using Whycespace.Engines.T0U.WhyceGovernance.Results;
using Whycespace.Domain.Events.Governance;

public sealed class QuorumEngine
{
    public (QuorumResult Result, GovernanceQuorumEvaluatedEvent? EvaluatedEvent, object? OutcomeEvent) Execute(EvaluateQuorumCommand command)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            var failResult = new QuorumResult(
                Success: false,
                ProposalId: command.ProposalId,
                ParticipationPercentage: 0m,
                ApprovalPercentage: 0m,
                QuorumMet: false,
                Message: validationError,
                ExecutedAt: DateTime.UtcNow);

            return (failResult, null, null);
        }

        var participationPercentage = (decimal)command.VotesCast / command.TotalEligibleGuardians * 100m;

        var approvalPercentage = command.VotesCast > 0
            ? (decimal)command.VotesApprove / command.VotesCast * 100m
            : 0m;

        var participationMet = participationPercentage >= command.RequiredParticipationPercentage;
        var approvalMet = approvalPercentage >= command.RequiredApprovalPercentage;
        var quorumMet = participationMet && approvalMet;

        var evaluatedAt = DateTime.UtcNow;

        var evaluatedEvent = new GovernanceQuorumEvaluatedEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            TotalEligibleGuardians: command.TotalEligibleGuardians,
            VotesCast: command.VotesCast,
            VotesApprove: command.VotesApprove,
            VotesReject: command.VotesReject,
            VotesAbstain: command.VotesAbstain,
            ParticipationPercentage: participationPercentage,
            ApprovalPercentage: approvalPercentage,
            EvaluatedAt: evaluatedAt);

        object? outcomeEvent;
        string message;

        if (quorumMet)
        {
            outcomeEvent = new GovernanceQuorumMetEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                ParticipationPercentage: participationPercentage,
                ApprovalPercentage: approvalPercentage,
                EvaluatedAt: evaluatedAt);
            message = "Quorum met";
        }
        else
        {
            var reasons = new List<string>();
            if (!participationMet)
                reasons.Add($"Participation {participationPercentage:F2}% below required {command.RequiredParticipationPercentage:F2}%");
            if (!approvalMet)
                reasons.Add($"Approval {approvalPercentage:F2}% below required {command.RequiredApprovalPercentage:F2}%");

            var failureReason = string.Join("; ", reasons);

            outcomeEvent = new GovernanceQuorumFailedEvent(
                EventId: Guid.NewGuid(),
                ProposalId: command.ProposalId,
                ParticipationPercentage: participationPercentage,
                ApprovalPercentage: approvalPercentage,
                FailureReason: failureReason,
                EvaluatedAt: evaluatedAt);
            message = $"Quorum not met: {failureReason}";
        }

        var result = new QuorumResult(
            Success: true,
            ProposalId: command.ProposalId,
            ParticipationPercentage: participationPercentage,
            ApprovalPercentage: approvalPercentage,
            QuorumMet: quorumMet,
            Message: message,
            ExecutedAt: evaluatedAt);

        return (result, evaluatedEvent, outcomeEvent);
    }

    private static string? Validate(EvaluateQuorumCommand command)
    {
        if (command.TotalEligibleGuardians <= 0)
            return "Total eligible guardians must be greater than zero";

        if (command.VotesCast < 0 || command.VotesApprove < 0 || command.VotesReject < 0 || command.VotesAbstain < 0)
            return "Vote counts must not be negative";

        if (command.VotesCast > command.TotalEligibleGuardians)
            return "Votes cast cannot exceed total eligible guardians";

        if (command.VotesApprove + command.VotesReject + command.VotesAbstain != command.VotesCast)
            return "Vote breakdown must equal total votes cast";

        return null;
    }
}
