namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public interface IVaultPolicyEvaluationService
{
    VaultPolicyServiceResult Evaluate(
        string actorId,
        string domain,
        string operation,
        IReadOnlyDictionary<string, string> attributes);
}

public sealed record VaultPolicyServiceResult(
    bool Allowed,
    string Reason,
    IReadOnlyList<VaultPolicyDecisionEntry> Decisions,
    DateTime EvaluatedAt);

public sealed record VaultPolicyDecisionEntry(
    string PolicyId,
    bool Allowed,
    string Action,
    string Reason,
    DateTime EvaluatedAt);
