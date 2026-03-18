namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

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
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

public sealed class VaultIdentityAuthorizationAdapter
{
    private readonly IdentityRegistry _registry;
    private readonly AuthorizationEngine _authorizationEngine;
    private readonly IdentityRoleEngine _roleEngine;

    private static readonly Dictionary<string, string[]> RoleOperationPermissions = new()
    {
        ["Investor"] = ["Contribution", "ProfitDistribution"],
        ["Operator"] = ["Transfer", "Withdrawal"],
        ["TreasuryManager"] = ["Withdrawal", "Transfer", "ProfitDistribution"]
    };

    public VaultIdentityAuthorizationAdapter(
        IdentityRegistry registry,
        AuthorizationEngine authorizationEngine,
        IdentityRoleEngine roleEngine)
    {
        _registry = registry;
        _authorizationEngine = authorizationEngine;
        _roleEngine = roleEngine;
    }

    public VaultAuthorizationResult AuthorizeIdentity(VaultAuthorizationCommand command)
    {
        if (command.IdentityId == Guid.Empty)
            throw new ArgumentException("IdentityId is required", nameof(command));

        if (command.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.OperationType))
            throw new ArgumentException("OperationType is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.ParticipantRole))
            throw new ArgumentException("ParticipantRole is required", nameof(command));

        var evaluatedAt = DateTime.UtcNow;

        // Step 1: Verify identity existence
        if (!_registry.Exists(command.IdentityId))
        {
            return Denied(command, "Identity does not exist", evaluatedAt);
        }

        // Step 2: Verify identity status
        var identity = _registry.Get(command.IdentityId);

        if (identity.Status == IdentityStatus.Suspended)
        {
            return Denied(command, "Identity is suspended", evaluatedAt);
        }

        if (identity.Status == IdentityStatus.Revoked)
        {
            return Denied(command, "Identity is revoked", evaluatedAt);
        }

        if (identity.Status != IdentityStatus.Verified)
        {
            return Denied(command, $"Identity status is not active: {identity.Status}", evaluatedAt);
        }

        // Step 3: Verify participant role is assigned
        if (!_roleEngine.HasRole(command.IdentityId, command.ParticipantRole))
        {
            return Denied(command, $"Identity does not have role: {command.ParticipantRole}", evaluatedAt);
        }

        // Step 4: Verify operation permission for role
        if (!IsOperationPermittedForRole(command.ParticipantRole, command.OperationType))
        {
            return Denied(command,
                $"Role '{command.ParticipantRole}' is not permitted to perform '{command.OperationType}'",
                evaluatedAt);
        }

        // Step 5: Delegate to WhyceID authorization engine
        var authRequest = new AuthorizationRequest(
            IdentityId: command.IdentityId,
            Resource: $"vault:{command.VaultId}",
            Action: command.OperationType,
            Scope: "economic:vault");

        var authResult = _authorizationEngine.Authorize(authRequest);

        if (!authResult.Allowed)
        {
            return Denied(command, authResult.Reason, evaluatedAt);
        }

        return new VaultAuthorizationResult(
            IdentityId: command.IdentityId,
            VaultId: command.VaultId,
            OperationType: command.OperationType,
            IsAuthorized: true,
            AuthorizationReason: "Authorized",
            EvaluatedAt: evaluatedAt);
    }

    private static bool IsOperationPermittedForRole(string role, string operationType)
    {
        if (!RoleOperationPermissions.TryGetValue(role, out var allowedOperations))
            return false;

        return Array.Exists(allowedOperations, op => op == operationType);
    }

    private static VaultAuthorizationResult Denied(
        VaultAuthorizationCommand command, string reason, DateTime evaluatedAt)
    {
        return new VaultAuthorizationResult(
            IdentityId: command.IdentityId,
            VaultId: command.VaultId,
            OperationType: command.OperationType,
            IsAuthorized: false,
            AuthorizationReason: reason,
            EvaluatedAt: evaluatedAt);
    }
}
