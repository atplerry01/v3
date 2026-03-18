namespace Whycespace.Engines.T0U.Governance.ProposalType.Validation;

using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T0U.Governance.ProposalType.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Deactivation;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceProposalTypeEngine
{
    private readonly GovernanceProposalTypeStore _typeStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceProposalTypeEngine(
        GovernanceProposalTypeStore typeStore,
        GuardianRegistryStore guardianStore)
    {
        _typeStore = typeStore;
        _guardianStore = guardianStore;
    }

    public (GovernanceProposalTypeResult Result, GovernanceProposalTypeRegisteredEvent? Event) Execute(
        RegisterProposalTypeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProposalType))
            return Fail<GovernanceProposalTypeRegisteredEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Registered, "Proposal type is required.");

        if (string.IsNullOrWhiteSpace(command.Description))
            return Fail<GovernanceProposalTypeRegisteredEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Registered, "Description is required.");

        var guardian = _guardianStore.GetGuardian(command.RegisteredByGuardianId.ToString());
        if (guardian is null)
            return Fail<GovernanceProposalTypeRegisteredEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Registered,
                $"Guardian not found: {command.RegisteredByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail<GovernanceProposalTypeRegisteredEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Registered,
                $"Guardian is not active: {command.RegisteredByGuardianId}");

        if (_typeStore.Exists(command.ProposalType))
            return Fail<GovernanceProposalTypeRegisteredEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Registered,
                $"Proposal type already exists: {command.ProposalType}");

        var type = new GovernanceProposalType(command.ProposalType, command.ProposalType, command.Description);
        _typeStore.Add(type);

        var result = new GovernanceProposalTypeResult(
            true,
            command.ProposalType,
            GovernanceProposalTypeAction.Registered,
            string.Empty,
            $"Proposal type '{command.ProposalType}' registered successfully.",
            command.Timestamp);

        var domainEvent = new GovernanceProposalTypeRegisteredEvent(
            EventId: Guid.NewGuid(),
            ProposalType: command.ProposalType,
            Description: command.Description,
            RegisteredByGuardianId: command.RegisteredByGuardianId,
            RegisteredAt: command.Timestamp,
            EventVersion: 1);

        return (result, domainEvent);
    }

    public (GovernanceProposalTypeResult Result, GovernanceProposalTypeDeactivatedEvent? Event) Execute(
        DeactivateProposalTypeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProposalType))
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated, "Proposal type is required.");

        if (string.IsNullOrWhiteSpace(command.Reason))
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated, "Reason is required.");

        var guardian = _guardianStore.GetGuardian(command.DeactivatedByGuardianId.ToString());
        if (guardian is null)
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated,
                $"Guardian not found: {command.DeactivatedByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated,
                $"Guardian is not active: {command.DeactivatedByGuardianId}");

        var existing = _typeStore.Get(command.ProposalType);
        if (existing is null)
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated,
                $"Proposal type not found: {command.ProposalType}");

        if (!existing.IsActive)
            return Fail<GovernanceProposalTypeDeactivatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Deactivated,
                $"Proposal type already deactivated: {command.ProposalType}");

        var updated = existing with { IsActive = false };
        _typeStore.Update(updated);

        var result = new GovernanceProposalTypeResult(
            true,
            command.ProposalType,
            GovernanceProposalTypeAction.Deactivated,
            string.Empty,
            $"Proposal type '{command.ProposalType}' deactivated. Reason: {command.Reason}",
            command.Timestamp);

        var domainEvent = new GovernanceProposalTypeDeactivatedEvent(
            EventId: Guid.NewGuid(),
            ProposalType: command.ProposalType,
            Reason: command.Reason,
            DeactivatedByGuardianId: command.DeactivatedByGuardianId,
            DeactivatedAt: command.Timestamp,
            EventVersion: 1);

        return (result, domainEvent);
    }

    public (GovernanceProposalTypeResult Result, GovernanceProposalTypeValidatedEvent? Event) Execute(
        ValidateProposalTypeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProposalType))
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated, "Proposal type is required.");

        if (string.IsNullOrWhiteSpace(command.AuthorityDomain))
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated, "Authority domain is required.");

        var guardian = _guardianStore.GetGuardian(command.RequestedByGuardianId.ToString());
        if (guardian is null)
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated,
                $"Guardian not found: {command.RequestedByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated,
                $"Guardian is not active: {command.RequestedByGuardianId}");

        var existing = _typeStore.Get(command.ProposalType);
        if (existing is null)
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated,
                $"Proposal type not registered: {command.ProposalType}");

        if (!existing.IsActive)
            return Fail<GovernanceProposalTypeValidatedEvent>(
                command.ProposalType, GovernanceProposalTypeAction.Validated,
                $"Proposal type is deactivated: {command.ProposalType}");

        var result = new GovernanceProposalTypeResult(
            true,
            command.ProposalType,
            GovernanceProposalTypeAction.Validated,
            command.AuthorityDomain,
            $"Proposal type '{command.ProposalType}' is valid for domain '{command.AuthorityDomain}'.",
            command.Timestamp);

        var domainEvent = new GovernanceProposalTypeValidatedEvent(
            EventId: Guid.NewGuid(),
            ProposalType: command.ProposalType,
            AuthorityDomain: command.AuthorityDomain,
            ValidatedByGuardianId: command.RequestedByGuardianId,
            ValidatedAt: command.Timestamp,
            EventVersion: 1);

        return (result, domainEvent);
    }

    public GovernanceProposalType GetType(string typeId)
    {
        return _typeStore.Get(typeId)
            ?? throw new KeyNotFoundException($"Proposal type not found: {typeId}");
    }

    public IReadOnlyList<GovernanceProposalType> ListTypes()
    {
        return _typeStore.ListAll();
    }

    private static (GovernanceProposalTypeResult, T?) Fail<T>(
        string proposalType, GovernanceProposalTypeAction action, string message) where T : class
    {
        return (new GovernanceProposalTypeResult(
            false,
            proposalType ?? string.Empty,
            action,
            string.Empty,
            message,
            DateTime.UtcNow), null);
    }
}
