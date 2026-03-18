namespace Whycespace.Domain.Governance;

public sealed record GovernanceRule(
    Guid RuleId,
    string Name,
    string Expression,
    GovernanceRuleSeverity Severity,
    bool Enforced,
    DateTimeOffset CreatedAt
);

public enum GovernanceRuleSeverity
{
    Advisory,
    Warning,
    Blocking
}
