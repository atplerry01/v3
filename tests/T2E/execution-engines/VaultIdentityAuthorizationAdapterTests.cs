namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T0U.WhyceID.Identity.Creation;
using Whycespace.Engines.T0U.WhyceID.Identity.Attributes;
using Whycespace.Engines.T0U.WhyceID.Identity.Graph;
using Whycespace.Engines.T0U.WhyceID.Authentication;
using Whycespace.Engines.T0U.WhyceID.Authorization.Decision;
using Whycespace.Engines.T0U.WhyceID.Consent;
using Whycespace.Engines.T0U.WhyceID.Trust.Device;
using Whycespace.Engines.T0U.WhyceID.Trust.Scoring;
using Whycespace.Engines.T0U.WhyceID.Federation.Provider;
using Whycespace.Engines.T0U.WhyceID.AccessScope.Assignment;
using Whycespace.Engines.T0U.WhyceID.Audit.Reporting;
using Whycespace.Engines.T0U.WhyceID.Recovery.Execution;
using Whycespace.Engines.T0U.WhyceID.Revocation.Execution;
using Whycespace.Engines.T0U.WhyceID.Roles.Assignment;
using Whycespace.Engines.T0U.WhyceID.Permissions.Grant;
using Whycespace.Engines.T0U.WhyceID.Policy.Enforcement;
using Whycespace.Engines.T0U.WhyceID.Verification.Identity;
using Whycespace.Engines.T0U.WhyceID.Service.Registration;
using Whycespace.Engines.T0U.WhyceID.Session.Creation;
using Whycespace.Engines.T2E.Economic.Vault.Adapters;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class VaultIdentityAuthorizationAdapterTests
{
    private readonly IdentityRegistry _registry = new();
    private readonly IdentityRoleStore _roleStore = new();
    private readonly IdentityPermissionStore _permissionStore = new();
    private readonly IdentityAccessScopeStore _scopeStore = new();
    private readonly VaultIdentityAuthorizationAdapter _adapter;

    public VaultIdentityAuthorizationAdapterTests()
    {
        var roleEngine = new IdentityRoleEngine(_registry, _roleStore);
        var permissionEngine = new IdentityPermissionEngine(_permissionStore);
        var scopeEngine = new IdentityAccessScopeEngine(_scopeStore);
        var authorizationEngine = new AuthorizationEngine(_registry, roleEngine, permissionEngine, scopeEngine);

        _adapter = new VaultIdentityAuthorizationAdapter(_registry, authorizationEngine, roleEngine);
    }

    private IdentityAggregate RegisterVerifiedIdentity(Guid identityId, string role)
    {
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        identity.Verify();
        _registry.Register(identity);
        _roleStore.Assign(identityId, role);
        _permissionStore.Assign(role, $"vault:{identityId}:{role}");
        _scopeStore.Assign(role, "economic:vault");
        return identity;
    }

    private void SetupPermissionsForRole(Guid identityId, string role, string operationType)
    {
        var permission = $"vault:{identityId}:{operationType}";
        _permissionStore.Assign(role, permission);
        _scopeStore.Assign(role, "economic:vault");
    }

    private static VaultAuthorizationCommand CreateCommand(
        Guid identityId, string operationType, string participantRole)
    {
        return new VaultAuthorizationCommand(
            IdentityId: identityId,
            VaultId: Guid.NewGuid(),
            VaultAccountId: Guid.NewGuid(),
            OperationType: operationType,
            ParticipantRole: participantRole,
            RequestedAt: DateTime.UtcNow);
    }

    [Fact]
    public void AuthorizedIdentity_ShouldPass()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "Investor");

        var command = CreateCommand(identityId, "Contribution", "Investor");
        _permissionStore.Assign("Investor", $"vault:{command.VaultId}:Contribution");
        _scopeStore.Assign("Investor", "economic:vault");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.True(result.IsAuthorized);
        Assert.Equal("Authorized", result.AuthorizationReason);
        Assert.Equal(identityId, result.IdentityId);
        Assert.Equal(command.VaultId, result.VaultId);
    }

    [Fact]
    public void NonExistentIdentity_ShouldBeRejected()
    {
        var command = CreateCommand(Guid.NewGuid(), "Contribution", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Equal("Identity does not exist", result.AuthorizationReason);
    }

    [Fact]
    public void SuspendedIdentity_ShouldBeRejected()
    {
        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        identity.Verify();
        identity.Suspend();
        _registry.Register(identity);

        var command = CreateCommand(identityId, "Contribution", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Equal("Identity is suspended", result.AuthorizationReason);
    }

    [Fact]
    public void RevokedIdentity_ShouldBeRejected()
    {
        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        identity.Verify();
        identity.Revoke();
        _registry.Register(identity);

        var command = CreateCommand(identityId, "Contribution", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Equal("Identity is revoked", result.AuthorizationReason);
    }

    [Fact]
    public void PendingIdentity_ShouldBeRejected()
    {
        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        _registry.Register(identity);

        var command = CreateCommand(identityId, "Contribution", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Contains("not active", result.AuthorizationReason);
    }

    [Fact]
    public void InvestorCanContribute_ShouldAuthorize()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "Investor");

        var command = CreateCommand(identityId, "Contribution", "Investor");
        _permissionStore.Assign("Investor", $"vault:{command.VaultId}:Contribution");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public void InvestorCannotWithdraw_ShouldDeny()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "Investor");

        var command = CreateCommand(identityId, "Withdrawal", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Contains("not permitted", result.AuthorizationReason);
    }

    [Fact]
    public void OperatorCanTransfer_ShouldAuthorize()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "Operator");

        var command = CreateCommand(identityId, "Transfer", "Operator");
        _permissionStore.Assign("Operator", $"vault:{command.VaultId}:Transfer");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public void TreasuryManagerCanWithdraw_ShouldAuthorize()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "TreasuryManager");

        var command = CreateCommand(identityId, "Withdrawal", "TreasuryManager");
        _permissionStore.Assign("TreasuryManager", $"vault:{command.VaultId}:Withdrawal");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.True(result.IsAuthorized);
    }

    [Fact]
    public void IdentityWithoutRole_ShouldBeRejected()
    {
        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        identity.Verify();
        _registry.Register(identity);

        var command = CreateCommand(identityId, "Contribution", "Investor");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.False(result.IsAuthorized);
        Assert.Contains("does not have role", result.AuthorizationReason);
    }

    [Fact]
    public void ResultContainsCorrectMetadata()
    {
        var identityId = Guid.NewGuid();
        RegisterVerifiedIdentity(identityId, "Investor");

        var command = CreateCommand(identityId, "Contribution", "Investor");
        _permissionStore.Assign("Investor", $"vault:{command.VaultId}:Contribution");

        var result = _adapter.AuthorizeIdentity(command);

        Assert.Equal(identityId, result.IdentityId);
        Assert.Equal(command.VaultId, result.VaultId);
        Assert.Equal("Contribution", result.OperationType);
        Assert.True(result.EvaluatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void EmptyIdentityId_ShouldThrow()
    {
        var command = new VaultAuthorizationCommand(
            IdentityId: Guid.Empty,
            VaultId: Guid.NewGuid(),
            VaultAccountId: Guid.NewGuid(),
            OperationType: "Contribution",
            ParticipantRole: "Investor",
            RequestedAt: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _adapter.AuthorizeIdentity(command));
    }

    [Fact]
    public void EmptyVaultId_ShouldThrow()
    {
        var command = new VaultAuthorizationCommand(
            IdentityId: Guid.NewGuid(),
            VaultId: Guid.Empty,
            VaultAccountId: Guid.NewGuid(),
            OperationType: "Contribution",
            ParticipantRole: "Investor",
            RequestedAt: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _adapter.AuthorizeIdentity(command));
    }
}
