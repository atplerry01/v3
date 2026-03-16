namespace Whycespace.Engines.T1M.WSS.Stores;

using global::System.Collections.Concurrent;
using global::System.Text.RegularExpressions;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowVersionStore
{
    private static readonly Regex SemVerPattern = new(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, SortedDictionary<string, WorkflowDefinition>> _store = new();

    public void Store(WorkflowDefinition workflow)
    {
        var versions = _store.GetOrAdd(workflow.WorkflowId, _ => new SortedDictionary<string, WorkflowDefinition>(new SemanticVersionComparer()));

        lock (versions)
        {
            if (versions.ContainsKey(workflow.Version))
                throw new InvalidOperationException($"Version {workflow.Version} already exists for workflow: {workflow.WorkflowId}");

            versions[workflow.Version] = workflow;
        }
    }

    public WorkflowDefinition? Get(string workflowId, string version)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return null;

        lock (versions)
        {
            return versions.TryGetValue(version, out var workflow) ? workflow : null;
        }
    }

    public WorkflowDefinition? GetLatest(string workflowId)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return null;

        lock (versions)
        {
            return versions.Count > 0 ? versions.Values.Last() : null;
        }
    }

    public IReadOnlyList<WorkflowDefinition> GetVersions(string workflowId)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return Array.Empty<WorkflowDefinition>();

        lock (versions)
        {
            return versions.Values.ToList();
        }
    }

    public bool VersionExists(string workflowId, string version)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return false;

        lock (versions)
        {
            return versions.ContainsKey(version);
        }
    }

    public static bool IsValidSemanticVersion(string version)
    {
        return SemVerPattern.IsMatch(version);
    }

    private sealed class SemanticVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var partsX = x.Split('.');
            var partsY = y.Split('.');

            for (var i = 0; i < 3; i++)
            {
                var px = i < partsX.Length && int.TryParse(partsX[i], out var vx) ? vx : 0;
                var py = i < partsY.Length && int.TryParse(partsY[i], out var vy) ? vy : 0;

                if (px != py)
                    return px.CompareTo(py);
            }

            return 0;
        }
    }
}
