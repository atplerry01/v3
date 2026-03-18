using Whycespace.Engines.T3I.Forecasting.Policy.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Forecasting.Policy.Engines;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyImpactForecastEngine : IIntelligenceEngine<PolicyImpactForecastInput, PolicyImpactForecastResult>
{
    public string EngineName => "PolicyImpactForecast";

    public IntelligenceResult<PolicyImpactForecastResult> Execute(IntelligenceContext<PolicyImpactForecastInput> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var input = context.Input;

        var records = new List<PolicyImpactRecord>();
        var affectedPolicies = new HashSet<string>();

        foreach (var simContext in input.SimulationContexts)
        {
            var currentDecision = EvaluatePolicies(input.CurrentPolicies, simContext);
            var proposedDecision = EvaluatePolicies(input.ProposedPolicies, simContext);

            var impactType = ClassifyImpact(currentDecision, proposedDecision);
            var severity = AssignSeverity(impactType, currentDecision, proposedDecision);

            records.Add(new PolicyImpactRecord(
                simContext.ContextId,
                currentDecision,
                proposedDecision,
                impactType,
                severity
            ));

            if (impactType != ImpactType.NO_CHANGE)
            {
                foreach (var policy in input.ProposedPolicies)
                {
                    if (MatchesContext(policy, simContext))
                        affectedPolicies.Add(policy.PolicyId);
                }
            }
        }

        var orderedRecords = records
            .OrderBy(r => r.ContextId)
            .ToList();

        var sortedAffected = affectedPolicies
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var riskLevel = ComputeOverallRisk(orderedRecords);

        var result = new PolicyImpactForecastResult(
            orderedRecords,
            sortedAffected,
            riskLevel,
            DateTime.UtcNow
        );

        return IntelligenceResult<PolicyImpactForecastResult>.Ok(result,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static string EvaluatePolicies(IReadOnlyList<PolicyDefinition> policies, PolicyContext context)
    {
        var matchingPolicies = policies
            .Where(p => MatchesContext(p, context))
            .OrderBy(p => p.PolicyId, StringComparer.Ordinal)
            .ToList();

        if (matchingPolicies.Count == 0)
            return "no_match";

        var hasExplicitDeny = matchingPolicies.Any(p =>
            p.Actions.Any(a => a.ActionType == "deny"));

        if (hasExplicitDeny)
            return "deny";

        var hasEscalation = matchingPolicies.Any(p =>
            p.Actions.Any(a => a.ActionType == "escalate" || a.ActionType == "require_guardian"));

        if (hasEscalation)
            return "escalate";

        var hasAllow = matchingPolicies.Any(p =>
            p.Actions.Any(a => a.ActionType == "allow" || a.ActionType == "auto_approve"));

        if (hasAllow)
            return "allow";

        return matchingPolicies[0].Actions.Count > 0
            ? matchingPolicies[0].Actions[0].ActionType
            : "no_action";
    }

    private static bool MatchesContext(PolicyDefinition policy, PolicyContext context)
    {
        if (policy.TargetDomain != context.TargetDomain)
            return false;

        foreach (var condition in policy.Conditions)
        {
            if (!context.Attributes.TryGetValue(condition.Field, out var contextValue))
                return false;

            var matches = condition.Operator switch
            {
                "equals" => contextValue == condition.Value,
                "not_equals" => contextValue != condition.Value,
                "contains" => contextValue.Contains(condition.Value, StringComparison.Ordinal),
                "starts_with" => contextValue.StartsWith(condition.Value, StringComparison.Ordinal),
                "ends_with" => contextValue.EndsWith(condition.Value, StringComparison.Ordinal),
                _ => contextValue == condition.Value
            };

            if (!matches)
                return false;
        }

        return true;
    }

    private static ImpactType ClassifyImpact(string currentDecision, string proposedDecision)
    {
        if (currentDecision == proposedDecision)
            return ImpactType.NO_CHANGE;

        if (IsAccessDecision(currentDecision) && IsAccessDecision(proposedDecision))
            return ImpactType.ACCESS_CHANGE;

        if (IsEscalationDecision(currentDecision) != IsEscalationDecision(proposedDecision))
            return ImpactType.ESCALATION_CHANGE;

        if (IsGovernanceDecision(currentDecision) || IsGovernanceDecision(proposedDecision))
            return ImpactType.GOVERNANCE_CHANGE;

        return ImpactType.DECISION_CHANGE;
    }

    private static ImpactSeverity AssignSeverity(ImpactType impactType, string currentDecision, string proposedDecision)
    {
        if (impactType == ImpactType.NO_CHANGE)
            return ImpactSeverity.LOW;

        if (currentDecision == "deny" && proposedDecision == "allow")
            return ImpactSeverity.CRITICAL;

        if (currentDecision == "allow" && proposedDecision == "deny")
            return ImpactSeverity.HIGH;

        return impactType switch
        {
            ImpactType.GOVERNANCE_CHANGE => ImpactSeverity.HIGH,
            ImpactType.ESCALATION_CHANGE => ImpactSeverity.MEDIUM,
            ImpactType.ACCESS_CHANGE => ImpactSeverity.MEDIUM,
            ImpactType.DECISION_CHANGE => ImpactSeverity.MEDIUM,
            _ => ImpactSeverity.LOW
        };
    }

    private static ImpactSeverity ComputeOverallRisk(IReadOnlyList<PolicyImpactRecord> records)
    {
        if (records.Count == 0)
            return ImpactSeverity.LOW;

        if (records.Any(r => r.Severity == ImpactSeverity.CRITICAL))
            return ImpactSeverity.CRITICAL;

        if (records.Any(r => r.Severity == ImpactSeverity.HIGH))
            return ImpactSeverity.HIGH;

        if (records.Any(r => r.Severity == ImpactSeverity.MEDIUM))
            return ImpactSeverity.MEDIUM;

        return ImpactSeverity.LOW;
    }

    private static bool IsAccessDecision(string decision)
        => decision is "allow" or "deny";

    private static bool IsEscalationDecision(string decision)
        => decision is "escalate" or "require_guardian";

    private static bool IsGovernanceDecision(string decision)
        => decision is "governance_review" or "constitutional_review";
}
