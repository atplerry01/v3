namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed class VaultPolicyEnforcementAdapter
{
    private readonly IVaultPolicyEvaluationService _policyService;

    public VaultPolicyEnforcementAdapter(IVaultPolicyEvaluationService policyService)
    {
        _policyService = policyService;
    }

    public VaultPolicyEvaluationResult EvaluatePolicy(VaultPolicyEvaluationCommand command)
    {
        if (command.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required", nameof(command));

        if (command.InitiatorIdentityId == Guid.Empty)
            throw new ArgumentException("InitiatorIdentityId is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.OperationType))
            throw new ArgumentException("OperationType is required", nameof(command));

        var attributes = BuildPolicyAttributes(command);

        var enforcementResult = _policyService.Evaluate(
            actorId: command.InitiatorIdentityId.ToString(),
            domain: "economic.vault",
            operation: command.OperationType,
            attributes: attributes);

        var decision = ResolveDecision(enforcementResult);

        return new VaultPolicyEvaluationResult(
            VaultId: command.VaultId,
            OperationType: command.OperationType,
            IsAllowed: enforcementResult.Allowed,
            PolicyDecision: decision,
            PolicyReason: enforcementResult.Reason,
            EvaluatedAt: enforcementResult.EvaluatedAt);
    }

    private static IReadOnlyDictionary<string, string> BuildPolicyAttributes(VaultPolicyEvaluationCommand command)
    {
        var attributes = new Dictionary<string, string>
        {
            ["vaultId"] = command.VaultId.ToString(),
            ["vaultAccountId"] = command.VaultAccountId.ToString(),
            ["operationType"] = command.OperationType,
            ["amount"] = command.Amount.ToString("F2"),
            ["currency"] = command.Currency,
            ["vaultPurpose"] = command.VaultPurpose,
            ["requestedAt"] = command.RequestedAt.ToString("O")
        };

        if (command.ReferenceId is not null)
            attributes["referenceId"] = command.ReferenceId;

        if (command.ReferenceType is not null)
            attributes["referenceType"] = command.ReferenceType;

        return attributes;
    }

    private static string ResolveDecision(VaultPolicyServiceResult result)
    {
        if (result.Allowed)
            return "Allow";

        var hasConditional = result.Decisions.Any(d =>
            !d.Allowed && d.Action is "require_guardian" or "require_quorum" or "escalate");

        return hasConditional ? "Conditional" : "Deny";
    }
}
