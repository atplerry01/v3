namespace Whycespace.Engines.T3I.Reporting.Policy;

using Whycespace.Engines.T0U.WhycePolicy.Simulation;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyConflictAnalysisEngine
{
    public PolicyConflictAnalysisResult AnalyzeConflicts(PolicyConflictAnalysisInput input)
    {
        var policyMap = input.Policies
            .OrderBy(p => p.PolicyId)
            .ToDictionary(p => p.PolicyId);

        var conflictsByDomain = GroupConflictsByDomain(input.DetectedConflicts, policyMap);
        var priorityChains = DetectPriorityOverrideChains(input.Policies, input.DetectedConflicts);
        var escalationRisks = DetectEscalationRisks(input.DetectedConflicts, input.SimulationResults, policyMap);
        var clusters = ClusterRelatedConflicts(conflictsByDomain, input.DetectedConflicts, policyMap);

        return new PolicyConflictAnalysisResult(
            clusters,
            escalationRisks,
            priorityChains,
            DateTime.UtcNow
        );
    }

    private static Dictionary<string, List<PolicyConflictRecord>> GroupConflictsByDomain(
        IReadOnlyList<PolicyConflictRecord> conflicts,
        Dictionary<string, PolicyDefinition> policyMap)
    {
        var result = new Dictionary<string, List<PolicyConflictRecord>>();

        foreach (var conflict in conflicts)
        {
            var domain = ResolveDomain(conflict.PolicyA, conflict.PolicyB, policyMap);

            if (!result.TryGetValue(domain, out var list))
            {
                list = new List<PolicyConflictRecord>();
                result[domain] = list;
            }

            list.Add(conflict);
        }

        return result;
    }

    private static List<PriorityOverrideChain> DetectPriorityOverrideChains(
        IReadOnlyList<PolicyDefinition> policies,
        IReadOnlyList<PolicyConflictRecord> conflicts)
    {
        var chains = new List<PriorityOverrideChain>();
        var sortedPolicies = policies.OrderBy(p => p.PolicyId).ToList();

        var domainGroups = sortedPolicies
            .GroupBy(p => p.TargetDomain)
            .OrderBy(g => g.Key);

        foreach (var group in domainGroups)
        {
            var domainPolicies = group.OrderByDescending(p => p.Priority).ToList();

            if (domainPolicies.Count < 2)
                continue;

            var conflictPolicyIds = conflicts
                .Where(c => c.ConflictType == ConflictType.PRIORITY_CONFLICT ||
                            c.ConflictType == ConflictType.ACTION_CONFLICT)
                .SelectMany(c => new[] { c.PolicyA, c.PolicyB })
                .ToHashSet();

            var chainPolicies = domainPolicies
                .Where(p => conflictPolicyIds.Contains(p.PolicyId))
                .ToList();

            if (chainPolicies.Count < 2)
                continue;

            var chainIds = chainPolicies.Select(p => p.PolicyId).ToList();
            var priorities = chainPolicies.Select(p => $"{p.PolicyId}({p.Priority})");

            chains.Add(new PriorityOverrideChain(
                chainIds,
                group.Key,
                $"Priority override chain in domain '{group.Key}': {string.Join(" > ", priorities)}"
            ));
        }

        return chains.OrderBy(c => c.Domain).ToList();
    }

    private static List<EscalationRisk> DetectEscalationRisks(
        IReadOnlyList<PolicyConflictRecord> conflicts,
        IReadOnlyList<PolicySimulationRecord> simulationResults,
        Dictionary<string, PolicyDefinition> policyMap)
    {
        var risks = new List<EscalationRisk>();

        var policyConflictCounts = new Dictionary<string, int>();
        foreach (var conflict in conflicts)
        {
            policyConflictCounts[conflict.PolicyA] = policyConflictCounts.GetValueOrDefault(conflict.PolicyA) + 1;
            policyConflictCounts[conflict.PolicyB] = policyConflictCounts.GetValueOrDefault(conflict.PolicyB) + 1;
        }

        foreach (var (policyId, count) in policyConflictCounts.OrderBy(kv => kv.Key))
        {
            if (count < 2)
                continue;

            var domain = policyMap.TryGetValue(policyId, out var policy)
                ? policy.TargetDomain
                : "unknown";

            var severity = count switch
            {
                >= 5 => ConflictSeverity.CRITICAL,
                >= 3 => ConflictSeverity.HIGH,
                _ => ConflictSeverity.MEDIUM
            };

            risks.Add(new EscalationRisk(
                policyId,
                domain,
                "MULTI_CONFLICT_ESCALATION",
                severity,
                $"Policy '{policyId}' is involved in {count} conflicts, indicating systemic governance risk"
            ));
        }

        var simulationDenials = new Dictionary<string, int>();
        foreach (var sim in simulationResults)
        {
            foreach (var decision in sim.Decisions)
            {
                if (!decision.Allowed)
                {
                    simulationDenials[decision.PolicyId] =
                        simulationDenials.GetValueOrDefault(decision.PolicyId) + 1;
                }
            }
        }

        foreach (var (policyId, denialCount) in simulationDenials.OrderBy(kv => kv.Key))
        {
            if (denialCount < 2)
                continue;

            var alreadyFlagged = risks.Any(r => r.PolicyId == policyId);
            if (alreadyFlagged)
                continue;

            var domain = policyMap.TryGetValue(policyId, out var policy)
                ? policy.TargetDomain
                : "unknown";

            risks.Add(new EscalationRisk(
                policyId,
                domain,
                "SIMULATION_DENIAL_PATTERN",
                ConflictSeverity.MEDIUM,
                $"Policy '{policyId}' caused {denialCount} denials in simulation, suggesting potential deployment risk"
            ));
        }

        return risks;
    }

    private static List<PolicyConflictCluster> ClusterRelatedConflicts(
        Dictionary<string, List<PolicyConflictRecord>> conflictsByDomain,
        IReadOnlyList<PolicyConflictRecord> allConflicts,
        Dictionary<string, PolicyDefinition> policyMap)
    {
        var clusters = new List<PolicyConflictCluster>();
        var clusterIndex = 0;

        foreach (var (domain, domainConflicts) in conflictsByDomain.OrderBy(kv => kv.Key))
        {
            var conflictTypeGroups = domainConflicts
                .GroupBy(c => c.ConflictType)
                .OrderBy(g => g.Key);

            foreach (var group in conflictTypeGroups)
            {
                var policyIds = group
                    .SelectMany(c => new[] { c.PolicyA, c.PolicyB })
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                var analysisType = MapConflictType(group.Key);
                var severity = AssignSeverity(group.Key, group.Count(), policyIds.Count);

                var recommendation = GenerateRecommendation(analysisType, severity, policyIds.Count);

                clusters.Add(new PolicyConflictCluster(
                    $"cluster-{clusterIndex:D3}",
                    policyIds,
                    analysisType,
                    severity,
                    recommendation
                ));

                clusterIndex++;
            }
        }

        var crossDomainConflicts = DetectCrossDomainConflicts(allConflicts, policyMap);
        foreach (var crossDomain in crossDomainConflicts)
        {
            clusters.Add(new PolicyConflictCluster(
                $"cluster-{clusterIndex:D3}",
                crossDomain.Policies,
                AnalysisConflictType.DOMAIN_CONFLICT,
                crossDomain.Severity,
                crossDomain.Recommendation
            ));
            clusterIndex++;
        }

        return clusters;
    }

    private static List<(IReadOnlyList<string> Policies, ConflictSeverity Severity, string Recommendation)>
        DetectCrossDomainConflicts(
            IReadOnlyList<PolicyConflictRecord> conflicts,
            Dictionary<string, PolicyDefinition> policyMap)
    {
        var results = new List<(IReadOnlyList<string> Policies, ConflictSeverity Severity, string Recommendation)>();

        var crossDomainPairs = new List<(string PolicyA, string PolicyB)>();
        foreach (var conflict in conflicts)
        {
            var domainA = policyMap.TryGetValue(conflict.PolicyA, out var pA) ? pA.TargetDomain : null;
            var domainB = policyMap.TryGetValue(conflict.PolicyB, out var pB) ? pB.TargetDomain : null;

            if (domainA is not null && domainB is not null && domainA != domainB)
            {
                crossDomainPairs.Add((conflict.PolicyA, conflict.PolicyB));
            }
        }

        if (crossDomainPairs.Count == 0)
            return results;

        var policyIds = crossDomainPairs
            .SelectMany(p => new[] { p.PolicyA, p.PolicyB })
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var severity = crossDomainPairs.Count switch
        {
            >= 5 => ConflictSeverity.CRITICAL,
            >= 3 => ConflictSeverity.HIGH,
            _ => ConflictSeverity.MEDIUM
        };

        results.Add((
            policyIds,
            severity,
            $"Cross-domain conflict detected involving {policyIds.Count} policies across multiple domains. Review domain boundaries and policy scoping."
        ));

        return results;
    }

    private static AnalysisConflictType MapConflictType(ConflictType conflictType)
    {
        return conflictType switch
        {
            ConflictType.ACTION_CONFLICT => AnalysisConflictType.ACTION_CONFLICT,
            ConflictType.PRIORITY_CONFLICT => AnalysisConflictType.PRIORITY_CHAIN_CONFLICT,
            ConflictType.CONDITION_CONFLICT => AnalysisConflictType.DEPENDENCY_CONFLICT,
            ConflictType.LIFECYCLE_CONFLICT => AnalysisConflictType.DEPENDENCY_CONFLICT,
            _ => AnalysisConflictType.ACTION_CONFLICT
        };
    }

    private static ConflictSeverity AssignSeverity(ConflictType conflictType, int conflictCount, int policyCount)
    {
        var baseSeverity = conflictType switch
        {
            ConflictType.ACTION_CONFLICT => 2,
            ConflictType.PRIORITY_CONFLICT => 1,
            ConflictType.CONDITION_CONFLICT => 1,
            ConflictType.LIFECYCLE_CONFLICT => 0,
            _ => 0
        };

        var escalation = 0;
        if (conflictCount >= 5) escalation = 2;
        else if (conflictCount >= 3) escalation = 1;

        if (policyCount >= 5) escalation++;

        var total = Math.Min(baseSeverity + escalation, 3);

        return total switch
        {
            0 => ConflictSeverity.LOW,
            1 => ConflictSeverity.MEDIUM,
            2 => ConflictSeverity.HIGH,
            _ => ConflictSeverity.CRITICAL
        };
    }

    private static string GenerateRecommendation(AnalysisConflictType conflictType, ConflictSeverity severity, int policyCount)
    {
        var action = conflictType switch
        {
            AnalysisConflictType.ACTION_CONFLICT =>
                "Review contradicting policy actions and resolve allow/deny conflicts.",
            AnalysisConflictType.PRIORITY_CHAIN_CONFLICT =>
                "Reassign policy priorities to eliminate ambiguous override chains.",
            AnalysisConflictType.DEPENDENCY_CONFLICT =>
                "Audit policy dependencies and conditions for overlapping or circular logic.",
            AnalysisConflictType.DOMAIN_CONFLICT =>
                "Review domain boundaries and consider policy scope isolation.",
            _ =>
                "Review policy configuration."
        };

        var urgency = severity switch
        {
            ConflictSeverity.CRITICAL => "Immediate governance review required.",
            ConflictSeverity.HIGH => "Schedule governance review before next deployment.",
            ConflictSeverity.MEDIUM => "Include in next governance review cycle.",
            _ => "Monitor and address in routine maintenance."
        };

        return $"{action} {urgency} Affects {policyCount} policies.";
    }

    private static string ResolveDomain(string policyAId, string policyBId, Dictionary<string, PolicyDefinition> policyMap)
    {
        if (policyMap.TryGetValue(policyAId, out var policyA))
            return policyA.TargetDomain;

        if (policyMap.TryGetValue(policyBId, out var policyB))
            return policyB.TargetDomain;

        return "unknown";
    }
}
