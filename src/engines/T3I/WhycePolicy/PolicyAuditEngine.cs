namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using SystemEvidenceRecord = Whycespace.Systems.Upstream.WhycePolicy.Models.PolicyEvidenceRecord;

public sealed class PolicyAuditEngine
{
    public PolicyAuditReport GenerateAuditReport(
        PolicyAuditQuery query,
        IReadOnlyList<PolicyAuditRecord> auditRecords,
        IReadOnlyList<SystemEvidenceRecord>? evidenceRecords = null)
    {
        var filtered = auditRecords.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.PolicyId))
            filtered = filtered.Where(r => r.PolicyId == query.PolicyId);

        if (!string.IsNullOrWhiteSpace(query.ActorId))
            filtered = filtered.Where(r => r.ActorId == query.ActorId);

        if (!string.IsNullOrWhiteSpace(query.ActionType)
            && Enum.TryParse<PolicyAuditActionType>(query.ActionType, ignoreCase: true, out var actionType))
            filtered = filtered.Where(r => r.ActionType == actionType);

        if (query.From.HasValue)
            filtered = filtered.Where(r => r.Timestamp >= query.From.Value);

        if (query.To.HasValue)
            filtered = filtered.Where(r => r.Timestamp <= query.To.Value);

        var orderedRecords = filtered.OrderBy(r => r.Timestamp).ToList();

        var evidenceLookup = query.IncludeEvidence && evidenceRecords is not null
            ? evidenceRecords.ToDictionary(e => e.EvidenceId, e => e)
            : null;

        var entries = orderedRecords.Select(r =>
        {
            string? evidenceId = null;
            if (evidenceLookup is not null)
            {
                var match = evidenceRecords!.FirstOrDefault(e =>
                    e.PolicyId == r.PolicyId && e.ActorId == r.ActorId);
                evidenceId = match?.EvidenceId;
            }

            return new PolicyAuditEntry(
                r.AuditId,
                r.PolicyId,
                r.ActionType.ToString(),
                r.ActorId,
                r.Timestamp,
                evidenceId,
                r.Summary
            );
        }).ToList();

        return new PolicyAuditReport(
            EvidenceRecords: Array.Empty<SystemEvidenceRecord>(),
            TotalRecords: 0,
            GeneratedAt: DateTime.UtcNow,
            PolicyId: query.PolicyId,
            AuditEntries: entries,
            TotalEntries: entries.Count,
            EvidenceLinked: query.IncludeEvidence && evidenceLookup is not null
        );
    }

    public PolicyAuditRecord CreateAuditRecord(PolicyAuditInput input)
    {
        var timestamp = DateTime.UtcNow;
        var contextHash = PolicyAuditHashGenerator.GenerateContextHash(
            input.PolicyId, input.ActorId, input.EvaluationContext);
        var summary = BuildSummary(input);
        var auditId = PolicyAuditHashGenerator.GenerateAuditId(
            input.PolicyId, input.ActorId, input.ActionType, contextHash, timestamp);

        return new PolicyAuditRecord(
            auditId,
            input.PolicyId,
            input.ActionType,
            input.PolicyDecision,
            input.ActorId,
            timestamp,
            contextHash,
            summary
        );
    }

    private static string BuildSummary(PolicyAuditInput input)
    {
        var action = input.ActionType switch
        {
            PolicyAuditActionType.POLICY_CREATED =>
                $"Policy '{input.PolicyId}' created by actor '{input.ActorId}'.",
            PolicyAuditActionType.POLICY_UPDATED =>
                $"Policy '{input.PolicyId}' updated by actor '{input.ActorId}'.",
            PolicyAuditActionType.POLICY_EVALUATED =>
                $"Policy '{input.PolicyId}' evaluated for actor '{input.ActorId}' with decision '{input.PolicyDecision}'.",
            PolicyAuditActionType.POLICY_ENFORCED =>
                $"Policy '{input.PolicyId}' enforced for actor '{input.ActorId}'. Enforcement result: {input.EnforcementResult}.",
            PolicyAuditActionType.POLICY_APPROVED =>
                $"Policy '{input.PolicyId}' approved by actor '{input.ActorId}'.",
            PolicyAuditActionType.POLICY_ACTIVATED =>
                $"Policy '{input.PolicyId}' activated by actor '{input.ActorId}'.",
            PolicyAuditActionType.POLICY_SUSPENDED =>
                $"Policy '{input.PolicyId}' suspended by actor '{input.ActorId}'. Reason: {input.EnforcementResult}.",
            PolicyAuditActionType.POLICY_REVOKED =>
                $"Policy '{input.PolicyId}' revoked by actor '{input.ActorId}'. Reason: {input.EnforcementResult}.",
            PolicyAuditActionType.POLICY_SIMULATED =>
                $"Policy '{input.PolicyId}' simulated for actor '{input.ActorId}'. Simulated decision: '{input.PolicyDecision}'.",
            PolicyAuditActionType.POLICY_FORECASTED =>
                $"Policy '{input.PolicyId}' impact forecasted for actor '{input.ActorId}'. Forecast result: '{input.PolicyDecision}'.",
            PolicyAuditActionType.POLICY_ESCALATED =>
                $"Policy '{input.PolicyId}' escalated for actor '{input.ActorId}'. Escalation reason: {input.EnforcementResult}.",
            _ =>
                $"Policy '{input.PolicyId}' action '{input.ActionType}' recorded for actor '{input.ActorId}'."
        };

        return action;
    }
}
