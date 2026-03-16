namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceDomainScopeEngine
{
    private readonly GovernanceDomainScopeStore _scopeStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceDomainScopeEngine(
        GovernanceDomainScopeStore scopeStore,
        GuardianRegistryStore guardianStore)
    {
        _scopeStore = scopeStore;
        _guardianStore = guardianStore;
    }

    public (GovernanceDomainScopeResult Result, GovernanceDomainScopeRegisteredEvent? Event) Execute(
        RegisterDomainScopeCommand command)
    {
        var guardian = _guardianStore.GetGuardian(command.RegisteredByGuardianId.ToString());
        if (guardian is null)
            return Fail(command.AuthorityDomain, Guid.Empty, default, DomainScopeAction.Registered,
                $"Guardian not found: {command.RegisteredByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail(command.AuthorityDomain, Guid.Empty, default, DomainScopeAction.Registered,
                $"Guardian is not active: {command.RegisteredByGuardianId}");

        if (_scopeStore.ScopeExists(command.AuthorityDomain))
            return Fail(command.AuthorityDomain, Guid.Empty, default, DomainScopeAction.Registered,
                $"Domain scope already exists: {command.AuthorityDomain}");

        var scope = new GovernanceDomainScope(
            ScopeId: command.AuthorityDomain,
            Name: command.AuthorityDomain,
            Description: command.Description,
            IsActive: true,
            RegisteredByGuardianId: command.RegisteredByGuardianId,
            RegisteredAt: command.Timestamp,
            DeactivatedAt: null);

        _scopeStore.AddScope(scope);

        var result = new GovernanceDomainScopeResult(
            Success: true,
            AuthorityDomain: command.AuthorityDomain,
            ProposalId: Guid.Empty,
            ProposalType: default,
            Action: DomainScopeAction.Registered,
            Message: $"Domain scope registered: {command.AuthorityDomain}",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceDomainScopeRegisteredEvent(
            EventId: Guid.NewGuid(),
            AuthorityDomain: command.AuthorityDomain,
            Description: command.Description,
            RegisteredByGuardianId: command.RegisteredByGuardianId,
            RegisteredAt: command.Timestamp);

        return (result, domainEvent);
    }

    public (GovernanceDomainScopeResult Result, GovernanceDomainScopeDeactivatedEvent? Event) Execute(
        DeactivateDomainScopeCommand command)
    {
        var guardian = _guardianStore.GetGuardian(command.DeactivatedByGuardianId.ToString());
        if (guardian is null)
            return Fail<GovernanceDomainScopeDeactivatedEvent>(command.AuthorityDomain, Guid.Empty, default,
                DomainScopeAction.Deactivated, $"Guardian not found: {command.DeactivatedByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail<GovernanceDomainScopeDeactivatedEvent>(command.AuthorityDomain, Guid.Empty, default,
                DomainScopeAction.Deactivated, $"Guardian is not active: {command.DeactivatedByGuardianId}");

        var scope = _scopeStore.GetScope(command.AuthorityDomain);
        if (scope is null)
            return Fail<GovernanceDomainScopeDeactivatedEvent>(command.AuthorityDomain, Guid.Empty, default,
                DomainScopeAction.Deactivated, $"Domain scope not found: {command.AuthorityDomain}");

        if (!scope.IsActive)
            return Fail<GovernanceDomainScopeDeactivatedEvent>(command.AuthorityDomain, Guid.Empty, default,
                DomainScopeAction.Deactivated, $"Domain scope already deactivated: {command.AuthorityDomain}");

        _scopeStore.DeactivateScope(command.AuthorityDomain, command.Timestamp);

        var result = new GovernanceDomainScopeResult(
            Success: true,
            AuthorityDomain: command.AuthorityDomain,
            ProposalId: Guid.Empty,
            ProposalType: default,
            Action: DomainScopeAction.Deactivated,
            Message: $"Domain scope deactivated: {command.AuthorityDomain}",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceDomainScopeDeactivatedEvent(
            EventId: Guid.NewGuid(),
            AuthorityDomain: command.AuthorityDomain,
            Reason: command.Reason,
            DeactivatedByGuardianId: command.DeactivatedByGuardianId,
            DeactivatedAt: command.Timestamp);

        return (result, domainEvent);
    }

    public (GovernanceDomainScopeResult Result, GovernanceDomainScopeValidatedEvent? Event) Execute(
        ValidateDomainScopeCommand command)
    {
        var guardian = _guardianStore.GetGuardian(command.RequestedByGuardianId.ToString());
        if (guardian is null)
            return Fail<GovernanceDomainScopeValidatedEvent>(command.AuthorityDomain, command.ProposalId,
                command.ProposalType, DomainScopeAction.Validated,
                $"Guardian not found: {command.RequestedByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return Fail<GovernanceDomainScopeValidatedEvent>(command.AuthorityDomain, command.ProposalId,
                command.ProposalType, DomainScopeAction.Validated,
                $"Guardian is not active: {command.RequestedByGuardianId}");

        var scope = _scopeStore.GetScope(command.AuthorityDomain);
        if (scope is null)
            return Fail<GovernanceDomainScopeValidatedEvent>(command.AuthorityDomain, command.ProposalId,
                command.ProposalType, DomainScopeAction.Validated,
                $"Domain scope not found: {command.AuthorityDomain}");

        if (!scope.IsActive)
            return Fail<GovernanceDomainScopeValidatedEvent>(command.AuthorityDomain, command.ProposalId,
                command.ProposalType, DomainScopeAction.Validated,
                $"Domain scope is deactivated: {command.AuthorityDomain}");

        var result = new GovernanceDomainScopeResult(
            Success: true,
            AuthorityDomain: command.AuthorityDomain,
            ProposalId: command.ProposalId,
            ProposalType: command.ProposalType,
            Action: DomainScopeAction.Validated,
            Message: $"Domain scope validated for proposal {command.ProposalId}: {command.AuthorityDomain}",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceDomainScopeValidatedEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            AuthorityDomain: command.AuthorityDomain,
            ProposalType: command.ProposalType.ToString(),
            ValidatedByGuardianId: command.RequestedByGuardianId,
            ValidatedAt: command.Timestamp);

        return (result, domainEvent);
    }

    public IReadOnlyList<GovernanceDomainScope> ListDomains()
    {
        return _scopeStore.ListAll();
    }

    private static (GovernanceDomainScopeResult, GovernanceDomainScopeRegisteredEvent?) Fail(
        string domain, Guid proposalId, ProposalType proposalType, DomainScopeAction action, string message)
    {
        return (new GovernanceDomainScopeResult(false, domain, proposalId, proposalType, action, message, DateTime.UtcNow), null);
    }

    private static (GovernanceDomainScopeResult, T?) Fail<T>(
        string domain, Guid proposalId, ProposalType proposalType, DomainScopeAction action, string message) where T : class
    {
        return (new GovernanceDomainScopeResult(false, domain, proposalId, proposalType, action, message, DateTime.UtcNow), null);
    }
}
