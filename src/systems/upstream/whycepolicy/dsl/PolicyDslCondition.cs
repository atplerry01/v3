namespace Whycespace.Systems.Upstream.WhycePolicy.Dsl;

public sealed record PolicyDslCondition(
    string AttributeName,
    string Operator,
    string ExpectedValue
);
