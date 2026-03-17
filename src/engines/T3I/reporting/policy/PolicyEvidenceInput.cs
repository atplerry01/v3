namespace Whycespace.Engines.T3I.Reporting.Policy;

public sealed record PolicyEvidenceInput(
    string PolicyId,
    string ActionType,
    string ActorId,
    Dictionary<string, object> EvidenceContext,
    DateTime Timestamp
);

public static class PolicyEvidenceActionType
{
    public const string POLICY_CREATED = "POLICY_CREATED";
    public const string POLICY_UPDATED = "POLICY_UPDATED";
    public const string POLICY_EVALUATED = "POLICY_EVALUATED";
    public const string POLICY_ENFORCED = "POLICY_ENFORCED";
    public const string POLICY_ACTIVATED = "POLICY_ACTIVATED";
    public const string POLICY_REVOKED = "POLICY_REVOKED";
}
