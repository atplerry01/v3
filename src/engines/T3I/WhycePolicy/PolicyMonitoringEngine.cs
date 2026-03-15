namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyMonitoringEngine
{
    private const double ExcessiveDenialRateThreshold = 0.5;
    private const double HighDenialRateThreshold = 0.75;
    private const double FrequentEscalationThreshold = 0.3;
    private const double HighEscalationThreshold = 0.5;
    private const int ConflictSpikeThreshold = 3;
    private const int HighConflictSpikeThreshold = 5;

    public PolicyMonitoringReport AnalyzeMonitoringWindow(PolicyMonitoringInput input)
    {
        var policyDecisionsByPolicy = AggregatePolicyDecisions(input.PolicyDecisionRecords);
        var enforcementByPolicy = AggregateEnforcements(input.EnforcementRecords);

        var allPolicyIds = policyDecisionsByPolicy.Keys
            .Concat(enforcementByPolicy.Keys)
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var anomalies = new List<PolicyAnomalyRecord>();
        var totalEvaluations = 0;
        var totalDenials = 0;
        var totalAllowed = 0;
        var totalEscalations = 0;

        foreach (var policyId in allPolicyIds)
        {
            var decisions = policyDecisionsByPolicy.GetValueOrDefault(policyId, new List<PolicyDecision>());
            var enforcements = enforcementByPolicy.GetValueOrDefault(policyId, new List<PolicyEnforcementResult>());

            var policyEvalCount = decisions.Count + enforcements.Count;
            var policyDenials = decisions.Count(d => !d.Allowed)
                + enforcements.Count(e => !e.Allowed);
            var policyAllowed = decisions.Count(d => d.Allowed)
                + enforcements.Count(e => e.Allowed);
            var policyEscalations = decisions.Count(d => d.Action is "escalate" or "require_guardian");

            totalEvaluations += policyEvalCount;
            totalDenials += policyDenials;
            totalAllowed += policyAllowed;
            totalEscalations += policyEscalations;

            if (policyEvalCount == 0)
                continue;

            var denialRate = (double)policyDenials / policyEvalCount;
            var escalationRate = policyEvalCount > 0
                ? (double)policyEscalations / policyEvalCount
                : 0.0;

            DetectDenialAnomaly(policyId, denialRate, policyDenials, input.ObservationWindow, anomalies);
            DetectEscalationAnomaly(policyId, escalationRate, policyEscalations, input.ObservationWindow, anomalies);
            DetectUnexpectedActivation(policyId, decisions, input.ObservationWindow, anomalies);
        }

        DetectConflictSpikes(input.PolicyDecisionRecords, input.ObservationWindow, anomalies);

        var overallDenialRate = totalEvaluations > 0 ? (double)totalDenials / totalEvaluations : 0.0;
        var overallEscalationRate = totalEvaluations > 0 ? (double)totalEscalations / totalEvaluations : 0.0;

        var statistics = new PolicyDecisionStatistics(
            totalEvaluations,
            totalDenials,
            totalAllowed,
            totalEscalations,
            Math.Round(overallDenialRate, 4),
            Math.Round(overallEscalationRate, 4)
        );

        var sortedAnomalies = anomalies
            .OrderBy(a => a.PolicyId, StringComparer.Ordinal)
            .ThenBy(a => a.AnomalyType)
            .ToList();

        var reportPolicyId = allPolicyIds.Count == 1 ? allPolicyIds[0] : "aggregate";

        return new PolicyMonitoringReport(
            reportPolicyId,
            input.ObservationWindow,
            statistics,
            sortedAnomalies,
            DateTime.UtcNow
        );
    }

    private static Dictionary<string, List<PolicyDecision>> AggregatePolicyDecisions(
        IReadOnlyList<PolicyEvaluationResult> records)
    {
        var result = new Dictionary<string, List<PolicyDecision>>();

        foreach (var record in records)
        {
            foreach (var decision in record.Decisions)
            {
                if (!result.TryGetValue(decision.PolicyId, out var list))
                {
                    list = new List<PolicyDecision>();
                    result[decision.PolicyId] = list;
                }

                list.Add(decision);
            }
        }

        return result;
    }

    private static Dictionary<string, List<PolicyEnforcementResult>> AggregateEnforcements(
        IReadOnlyList<PolicyEnforcementResult> records)
    {
        var result = new Dictionary<string, List<PolicyEnforcementResult>>();

        foreach (var record in records)
        {
            foreach (var decision in record.Decisions)
            {
                if (!result.TryGetValue(decision.PolicyId, out var list))
                {
                    list = new List<PolicyEnforcementResult>();
                    result[decision.PolicyId] = list;
                }

                list.Add(record);
            }
        }

        return result;
    }

    private static void DetectDenialAnomaly(
        string policyId,
        double denialRate,
        int denialCount,
        TimeRange window,
        List<PolicyAnomalyRecord> anomalies)
    {
        if (denialRate < ExcessiveDenialRateThreshold)
            return;

        var severity = denialRate >= HighDenialRateThreshold
            ? PolicyAnomalySeverity.CRITICAL
            : PolicyAnomalySeverity.HIGH;

        anomalies.Add(new PolicyAnomalyRecord(
            policyId,
            PolicyAnomalyType.EXCESSIVE_DENIAL_RATE,
            severity,
            $"Policy '{policyId}' has denial rate {denialRate:P1} ({denialCount} denials) during observation window {window.Start:O} to {window.End:O}",
            DateTime.UtcNow
        ));
    }

    private static void DetectEscalationAnomaly(
        string policyId,
        double escalationRate,
        int escalationCount,
        TimeRange window,
        List<PolicyAnomalyRecord> anomalies)
    {
        if (escalationRate < FrequentEscalationThreshold)
            return;

        var severity = escalationRate >= HighEscalationThreshold
            ? PolicyAnomalySeverity.HIGH
            : PolicyAnomalySeverity.MEDIUM;

        anomalies.Add(new PolicyAnomalyRecord(
            policyId,
            PolicyAnomalyType.FREQUENT_ESCALATION,
            severity,
            $"Policy '{policyId}' has escalation rate {escalationRate:P1} ({escalationCount} escalations) during observation window {window.Start:O} to {window.End:O}",
            DateTime.UtcNow
        ));
    }

    private static void DetectUnexpectedActivation(
        string policyId,
        List<PolicyDecision> decisions,
        TimeRange window,
        List<PolicyAnomalyRecord> anomalies)
    {
        var actionGroups = decisions
            .GroupBy(d => d.Action)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        if (actionGroups.Count <= 1)
            return;

        var dominantAction = actionGroups.OrderByDescending(g => g.Count()).First();
        var dominantRatio = (double)dominantAction.Count() / decisions.Count;

        if (dominantRatio >= 0.9)
            return;

        var unexpectedActions = actionGroups
            .Where(g => g.Key != dominantAction.Key)
            .Select(g => $"{g.Key}({g.Count()})")
            .ToList();

        anomalies.Add(new PolicyAnomalyRecord(
            policyId,
            PolicyAnomalyType.DECISION_PATTERN_CHANGE,
            PolicyAnomalySeverity.MEDIUM,
            $"Policy '{policyId}' shows mixed decision patterns: dominant={dominantAction.Key}({dominantAction.Count()}), others={string.Join(", ", unexpectedActions)}",
            DateTime.UtcNow
        ));
    }

    private static void DetectConflictSpikes(
        IReadOnlyList<PolicyEvaluationResult> records,
        TimeRange window,
        List<PolicyAnomalyRecord> anomalies)
    {
        var conflictingPolicies = new Dictionary<string, int>();

        foreach (var record in records)
        {
            var deniedIds = record.Decisions
                .Where(d => !d.Allowed)
                .Select(d => d.PolicyId)
                .ToList();

            var allowedIds = record.Decisions
                .Where(d => d.Allowed)
                .Select(d => d.PolicyId)
                .ToList();

            if (deniedIds.Count > 0 && allowedIds.Count > 0)
            {
                foreach (var id in deniedIds)
                {
                    conflictingPolicies[id] = conflictingPolicies.GetValueOrDefault(id) + 1;
                }
            }
        }

        foreach (var (policyId, count) in conflictingPolicies.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (count < ConflictSpikeThreshold)
                continue;

            var severity = count >= HighConflictSpikeThreshold
                ? PolicyAnomalySeverity.CRITICAL
                : PolicyAnomalySeverity.HIGH;

            anomalies.Add(new PolicyAnomalyRecord(
                policyId,
                PolicyAnomalyType.CONFLICT_SPIKE,
                severity,
                $"Policy '{policyId}' involved in {count} conflicting evaluation sets during observation window {window.Start:O} to {window.End:O}",
                DateTime.UtcNow
            ));
        }
    }
}
