namespace Whycespace.Systems.Upstream.WhycePolicy;

public sealed record PolicyRule(
    string RuleId,
    string Name,
    string Description,
    PolicySeverity Severity
);

public enum PolicySeverity { Info, Warning, Critical }

public sealed class PolicyGovernor
{
    private readonly List<PolicyRule> _rules = new();

    public void RegisterRule(PolicyRule rule) => _rules.Add(rule);

    public IReadOnlyList<PolicyRule> GetRules() => _rules;

    public bool EvaluatePolicy(string ruleId, IReadOnlyDictionary<string, object> context)
    {
        var rule = _rules.Find(r => r.RuleId == ruleId);
        return rule is not null;
    }
}
