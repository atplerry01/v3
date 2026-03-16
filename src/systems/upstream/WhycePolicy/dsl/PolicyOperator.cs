namespace Whycespace.Systems.Upstream.WhycePolicy.Dsl;

public static class PolicyOperator
{
    public new const string Equals = "equals";
    public const string NotEquals = "not_equals";
    public const string GreaterThan = "greater_than";
    public const string LessThan = "less_than";
    public const string Contains = "contains";
    public const string Exists = "exists";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Equals, NotEquals, GreaterThan, LessThan, Contains, Exists
    };

    public static bool IsValid(string op) => All.Contains(op);
}
