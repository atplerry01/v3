namespace Whycespace.Engines.T4A.WhycePolicy;

public sealed record PolicyEnforcementResult(
    bool Allowed,
    EnforcementAction EnforcementAction,
    string Reason,
    DateTime EnforcedAt
);

public enum EnforcementAction
{
    AllowExecution,
    DenyExecution,
    RequireGuardianApproval,
    RequireQuorumApproval,
    EscalateToGovernance
}
