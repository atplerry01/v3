namespace Whycespace.Engines.T0U.Governance.Dispute.Raising;

using Whycespace.Engines.T0U.Governance.Dispute.Resolution;
using Whycespace.Engines.T0U.Governance.Dispute.Withdrawal;

using Whycespace.Domain.Events.Governance;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceDisputeEngine
{
    private readonly GovernanceDisputeStore _disputeStore;
    private readonly GovernanceProposalStore _proposalStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceDisputeEngine(
        GovernanceDisputeStore disputeStore,
        GovernanceProposalStore proposalStore,
        GuardianRegistryStore guardianStore)
    {
        _disputeStore = disputeStore;
        _proposalStore = proposalStore;
        _guardianStore = guardianStore;
    }

    public (GovernanceDisputeResult Result, GovernanceDisputeRaisedEvent? Event) Execute(
        RaiseGovernanceDisputeCommand command)
    {
        var proposalId = command.ProposalId.ToString();
        var guardianId = command.RaisedByGuardianId.ToString();

        if (_proposalStore.Get(proposalId) is null)
            return Failure(command.ProposalId, command.DisputeType, $"Proposal not found: {proposalId}");

        if (!_guardianStore.Exists(guardianId))
            return Failure(command.ProposalId, command.DisputeType, $"Guardian not found: {guardianId}");

        if (string.IsNullOrWhiteSpace(command.DisputeReason))
            return Failure(command.ProposalId, command.DisputeType, "Dispute reason is required.");

        if (_disputeStore.ExistsForProposalAndGuardian(proposalId, guardianId))
            return Failure(command.ProposalId, command.DisputeType, "Dispute already exists for this proposal and guardian.");

        var disputeId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dispute = new GovernanceDispute(
            disputeId.ToString(),
            proposalId,
            guardianId,
            command.DisputeReason,
            command.DisputeType,
            DisputeStatus.Raised,
            EscalationLevel: 0,
            now,
            ResolvedAt: null);

        _disputeStore.Add(dispute);

        var result = new GovernanceDisputeResult(
            Success: true,
            DisputeId: disputeId,
            ProposalId: command.ProposalId,
            DisputeType: command.DisputeType,
            DisputeStatus: DisputeStatus.Raised,
            Message: "Dispute raised successfully.",
            ExecutedAt: now);

        var @event = new GovernanceDisputeRaisedEvent(
            EventId: Guid.NewGuid(),
            DisputeId: disputeId,
            ProposalId: command.ProposalId,
            DisputeType: command.DisputeType.ToString(),
            RaisedByGuardianId: command.RaisedByGuardianId,
            DisputeReason: command.DisputeReason,
            RaisedAt: now);

        return (result, @event);
    }

    public (GovernanceDisputeResult Result, GovernanceDisputeWithdrawnEvent? Event) Execute(
        WithdrawGovernanceDisputeCommand command)
    {
        var disputeId = command.DisputeId.ToString();
        var dispute = _disputeStore.Get(disputeId);

        if (dispute is null)
            return FailureFor(command.DisputeId, $"Dispute not found: {disputeId}");

        if (dispute.Status is DisputeStatus.Resolved or DisputeStatus.Withdrawn)
            return FailureFor(command.DisputeId, $"Cannot withdraw a dispute with status: {dispute.Status}");

        if (dispute.FiledBy != command.WithdrawnByGuardianId.ToString())
            return FailureFor(command.DisputeId, "Only the guardian who raised the dispute can withdraw it.");

        var now = DateTime.UtcNow;
        var updated = dispute with { Status = DisputeStatus.Withdrawn };
        _disputeStore.Update(updated);

        var result = new GovernanceDisputeResult(
            Success: true,
            DisputeId: command.DisputeId,
            ProposalId: Guid.Parse(dispute.ProposalId),
            DisputeType: dispute.DisputeType,
            DisputeStatus: DisputeStatus.Withdrawn,
            Message: "Dispute withdrawn successfully.",
            ExecutedAt: now);

        var @event = new GovernanceDisputeWithdrawnEvent(
            EventId: Guid.NewGuid(),
            DisputeId: command.DisputeId,
            WithdrawnByGuardianId: command.WithdrawnByGuardianId,
            Reason: command.Reason,
            WithdrawnAt: now);

        return (result, @event);
    }

    public (GovernanceDisputeResult Result, GovernanceDisputeResolvedEvent? Event) Execute(
        ResolveGovernanceDisputeCommand command)
    {
        var disputeId = command.DisputeId.ToString();
        var dispute = _disputeStore.Get(disputeId);

        if (dispute is null)
            return FailureResolve(command.DisputeId, $"Dispute not found: {disputeId}");

        if (dispute.Status == DisputeStatus.Resolved)
            return FailureResolve(command.DisputeId, "Dispute is already resolved.");

        if (dispute.Status == DisputeStatus.Withdrawn)
            return FailureResolve(command.DisputeId, "Cannot resolve a withdrawn dispute.");

        if (!_guardianStore.Exists(command.ResolvedByGuardianId.ToString()))
            return FailureResolve(command.DisputeId, $"Resolving guardian not found: {command.ResolvedByGuardianId}");

        var now = DateTime.UtcNow;
        var updated = dispute with
        {
            Status = DisputeStatus.Resolved,
            ResolvedAt = now
        };
        _disputeStore.Update(updated);

        var result = new GovernanceDisputeResult(
            Success: true,
            DisputeId: command.DisputeId,
            ProposalId: Guid.Parse(dispute.ProposalId),
            DisputeType: dispute.DisputeType,
            DisputeStatus: DisputeStatus.Resolved,
            Message: "Dispute resolved successfully.",
            ExecutedAt: now);

        var @event = new GovernanceDisputeResolvedEvent(
            EventId: Guid.NewGuid(),
            DisputeId: command.DisputeId,
            ResolutionOutcome: command.ResolutionOutcome,
            ResolvedByGuardianId: command.ResolvedByGuardianId,
            ResolvedAt: now);

        return (result, @event);
    }

    public (GovernanceDisputeResult Result, GovernanceDisputeEscalatedEvent? Event) Escalate(
        Guid disputeId, string escalationReason)
    {
        var dispute = _disputeStore.Get(disputeId.ToString());

        if (dispute is null)
            return FailureEscalate(disputeId, $"Dispute not found: {disputeId}");

        if (dispute.Status == DisputeStatus.Resolved)
            return FailureEscalate(disputeId, "Cannot escalate a resolved dispute.");

        if (dispute.Status == DisputeStatus.Withdrawn)
            return FailureEscalate(disputeId, "Cannot escalate a withdrawn dispute.");

        var now = DateTime.UtcNow;
        var updated = dispute with
        {
            Status = DisputeStatus.Escalated,
            EscalationLevel = dispute.EscalationLevel + 1
        };
        _disputeStore.Update(updated);

        var result = new GovernanceDisputeResult(
            Success: true,
            DisputeId: disputeId,
            ProposalId: Guid.Parse(dispute.ProposalId),
            DisputeType: dispute.DisputeType,
            DisputeStatus: DisputeStatus.Escalated,
            Message: $"Dispute escalated to level {updated.EscalationLevel}.",
            ExecutedAt: now);

        var @event = new GovernanceDisputeEscalatedEvent(
            EventId: Guid.NewGuid(),
            DisputeId: disputeId,
            EscalationReason: escalationReason,
            EscalatedAt: now);

        return (result, @event);
    }

    private static (GovernanceDisputeResult Result, GovernanceDisputeRaisedEvent? Event) Failure(
        Guid proposalId, DisputeType disputeType, string message)
    {
        return (new GovernanceDisputeResult(
            Success: false,
            DisputeId: Guid.Empty,
            ProposalId: proposalId,
            DisputeType: disputeType,
            DisputeStatus: DisputeStatus.Raised,
            Message: message,
            ExecutedAt: DateTime.UtcNow), null);
    }

    private static (GovernanceDisputeResult Result, GovernanceDisputeWithdrawnEvent? Event) FailureFor(
        Guid disputeId, string message)
    {
        return (new GovernanceDisputeResult(
            Success: false,
            DisputeId: disputeId,
            ProposalId: Guid.Empty,
            DisputeType: default,
            DisputeStatus: default,
            Message: message,
            ExecutedAt: DateTime.UtcNow), null);
    }

    private static (GovernanceDisputeResult Result, GovernanceDisputeResolvedEvent? Event) FailureResolve(
        Guid disputeId, string message)
    {
        return (new GovernanceDisputeResult(
            Success: false,
            DisputeId: disputeId,
            ProposalId: Guid.Empty,
            DisputeType: default,
            DisputeStatus: default,
            Message: message,
            ExecutedAt: DateTime.UtcNow), null);
    }

    private static (GovernanceDisputeResult Result, GovernanceDisputeEscalatedEvent? Event) FailureEscalate(
        Guid disputeId, string message)
    {
        return (new GovernanceDisputeResult(
            Success: false,
            DisputeId: disputeId,
            ProposalId: Guid.Empty,
            DisputeType: default,
            DisputeStatus: default,
            Message: message,
            ExecutedAt: DateTime.UtcNow), null);
    }
}
