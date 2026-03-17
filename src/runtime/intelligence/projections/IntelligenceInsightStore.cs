namespace Whycespace.IntelligenceRuntime.Projections;

using System.Collections.Concurrent;
using Whycespace.IntelligenceRuntime.Models;

public sealed class IntelligenceInsightStore
{
    private readonly ConcurrentDictionary<string, List<IntelligenceInsight>> _insights = new();

    public void Record(IntelligenceInsight insight)
    {
        _insights.AddOrUpdate(
            insight.Domain,
            [insight],
            (_, list) => { lock (list) { list.Add(insight); } return list; });
    }

    public IReadOnlyList<IntelligenceInsight> GetByDomain(string domain)
    {
        if (!_insights.TryGetValue(domain, out var list))
            return [];

        lock (list)
        {
            return list.ToList();
        }
    }

    public IReadOnlyList<IntelligenceInsight> GetRecent(int count = 50)
    {
        return _insights.Values
            .SelectMany(list => { lock (list) { return list.ToList(); } })
            .OrderByDescending(i => i.GeneratedAt)
            .Take(count)
            .ToList();
    }

    public int TotalCount => _insights.Values.Sum(list => { lock (list) { return list.Count; } });
}

public sealed record IntelligenceInsight(
    Guid InsightId,
    string Domain,
    IntelligenceCapability Capability,
    string EngineId,
    string Summary,
    IReadOnlyDictionary<string, object> Data,
    DateTimeOffset GeneratedAt
)
{
    public static IntelligenceInsight From(IntelligenceResult result, string domain, string summary)
        => new(
            Guid.NewGuid(),
            domain,
            result.Capability,
            result.EngineId,
            summary,
            result.Output,
            DateTimeOffset.UtcNow);
}
