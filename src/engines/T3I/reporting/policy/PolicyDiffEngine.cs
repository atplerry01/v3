namespace Whycespace.Engines.T3I.Reporting.Policy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyDiffEngine
{
    public PolicyDiffResult GenerateDiff(PolicyDiffInput input)
    {
        var changes = new List<PolicyChangeRecord>();

        CompareMetadata(input.PreviousPolicy, input.ProposedPolicy, changes);
        CompareConditions(input.PreviousPolicy.Conditions, input.ProposedPolicy.Conditions, changes);
        CompareActions(input.PreviousPolicy.Actions, input.ProposedPolicy.Actions, changes);
        ComparePriority(input.PreviousPolicy.Priority, input.ProposedPolicy.Priority, changes);
        CompareLifecycleState(input.PreviousPolicy.LifecycleState, input.ProposedPolicy.LifecycleState, changes);

        var orderedChanges = changes
            .OrderBy(c => c.FieldName, StringComparer.Ordinal)
            .ThenBy(c => c.ChangeType)
            .ToList();

        return new PolicyDiffResult(
            input.PreviousPolicy.PolicyId,
            orderedChanges,
            orderedChanges.Count,
            DateTime.UtcNow
        );
    }

    private static void CompareMetadata(PolicyDefinition previous, PolicyDefinition proposed, List<PolicyChangeRecord> changes)
    {
        if (previous.Name != proposed.Name)
        {
            changes.Add(new PolicyChangeRecord("Name", previous.Name, proposed.Name, PolicyChangeType.MODIFIED));
        }

        if (previous.Version != proposed.Version)
        {
            changes.Add(new PolicyChangeRecord("Version", previous.Version.ToString(), proposed.Version.ToString(), PolicyChangeType.MODIFIED));
        }

        if (previous.TargetDomain != proposed.TargetDomain)
        {
            changes.Add(new PolicyChangeRecord("TargetDomain", previous.TargetDomain, proposed.TargetDomain, PolicyChangeType.MODIFIED));
        }
    }

    private static void CompareConditions(
        IReadOnlyList<PolicyCondition> previous,
        IReadOnlyList<PolicyCondition> proposed,
        List<PolicyChangeRecord> changes)
    {
        var previousSet = previous
            .OrderBy(c => c.Field, StringComparer.Ordinal)
            .ThenBy(c => c.Operator, StringComparer.Ordinal)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .ToList();

        var proposedSet = proposed
            .OrderBy(c => c.Field, StringComparer.Ordinal)
            .ThenBy(c => c.Operator, StringComparer.Ordinal)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .ToList();

        var previousKeys = previousSet.Select(FormatCondition).ToHashSet(StringComparer.Ordinal);
        var proposedKeys = proposedSet.Select(FormatCondition).ToHashSet(StringComparer.Ordinal);

        foreach (var key in previousKeys.Except(proposedKeys).OrderBy(k => k, StringComparer.Ordinal))
        {
            changes.Add(new PolicyChangeRecord("Condition", key, null, PolicyChangeType.REMOVED));
        }

        foreach (var key in proposedKeys.Except(previousKeys).OrderBy(k => k, StringComparer.Ordinal))
        {
            changes.Add(new PolicyChangeRecord("Condition", null, key, PolicyChangeType.ADDED));
        }
    }

    private static void CompareActions(
        IReadOnlyList<PolicyAction> previous,
        IReadOnlyList<PolicyAction> proposed,
        List<PolicyChangeRecord> changes)
    {
        var previousKeys = previous.Select(FormatAction).OrderBy(k => k, StringComparer.Ordinal).ToList();
        var proposedKeys = proposed.Select(FormatAction).OrderBy(k => k, StringComparer.Ordinal).ToList();

        var previousSet = previousKeys.ToHashSet(StringComparer.Ordinal);
        var proposedSet = proposedKeys.ToHashSet(StringComparer.Ordinal);

        foreach (var key in previousSet.Except(proposedSet).OrderBy(k => k, StringComparer.Ordinal))
        {
            changes.Add(new PolicyChangeRecord("Action", key, null, PolicyChangeType.REMOVED));
        }

        foreach (var key in proposedSet.Except(previousSet).OrderBy(k => k, StringComparer.Ordinal))
        {
            changes.Add(new PolicyChangeRecord("Action", null, key, PolicyChangeType.ADDED));
        }
    }

    private static void ComparePriority(PolicyPriority previous, PolicyPriority proposed, List<PolicyChangeRecord> changes)
    {
        if (previous != proposed)
        {
            changes.Add(new PolicyChangeRecord("Priority", previous.ToString(), proposed.ToString(), PolicyChangeType.MODIFIED));
        }
    }

    private static void CompareLifecycleState(PolicyLifecycleState previous, PolicyLifecycleState proposed, List<PolicyChangeRecord> changes)
    {
        if (previous != proposed)
        {
            changes.Add(new PolicyChangeRecord("LifecycleState", previous.ToString(), proposed.ToString(), PolicyChangeType.MODIFIED));
        }
    }

    private static string FormatCondition(PolicyCondition c)
        => $"{c.Field}:{c.Operator}:{c.Value}";

    private static string FormatAction(PolicyAction a)
    {
        var parameters = a.Parameters.Count > 0
            ? string.Join(",", a.Parameters.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"))
            : "";
        return $"{a.ActionType}({parameters})";
    }
}
