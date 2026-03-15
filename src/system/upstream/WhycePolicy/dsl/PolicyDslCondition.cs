namespace Whycespace.System.Upstream.WhycePolicy.Dsl;

public sealed record PolicyDslCondition(
    string AttributeName,
    string Operator,
    string ExpectedValue
);
