namespace Whycespace.Systems.Midstream.WhycePlus;

public sealed record PlanningDirective(
    string DirectiveId,
    string Scope,
    string Action,
    IReadOnlyDictionary<string, object> Parameters,
    DateTimeOffset IssuedAt
);

public sealed class SystemPlanner
{
    private readonly List<PlanningDirective> _directives = new();

    public void IssueDirective(PlanningDirective directive) => _directives.Add(directive);

    public IReadOnlyList<PlanningDirective> GetDirectives(string? scope = null)
    {
        return scope is null
            ? _directives
            : _directives.Where(d => d.Scope == scope).ToList();
    }
}
