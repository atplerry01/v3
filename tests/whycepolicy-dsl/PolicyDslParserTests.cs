using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyDslParserTests
{
    private readonly PolicyDslParserEngine _engine = new();

    private const string ValidDsl = """
        POLICY "min-trust-score"
        NAME "Minimum Trust Score"
        VERSION 1
        DOMAIN "identity"
        WHEN trust_score less_than "50"
        THEN deny reason="Trust score too low"
        """;

    [Fact]
    public void Parse_ValidDsl_ReturnsPolicyDefinition()
    {
        var result = _engine.Parse(ValidDsl);

        Assert.Equal("min-trust-score", result.PolicyId);
        Assert.Equal("Minimum Trust Score", result.Name);
        Assert.Equal(1, result.Version);
        Assert.Equal("identity", result.TargetDomain);
        Assert.Single(result.Conditions);
        Assert.Single(result.Actions);
    }

    [Fact]
    public void Parse_ValidDsl_ParsesConditionCorrectly()
    {
        var result = _engine.Parse(ValidDsl);

        var condition = result.Conditions[0];
        Assert.Equal("trust_score", condition.Field);
        Assert.Equal("less_than", condition.Operator);
        Assert.Equal("50", condition.Value);
    }

    [Fact]
    public void Parse_ValidDsl_ParsesActionCorrectly()
    {
        var result = _engine.Parse(ValidDsl);

        var action = result.Actions[0];
        Assert.Equal("deny", action.ActionType);
        Assert.Equal("Trust score too low", action.Parameters["reason"]);
    }

    [Fact]
    public void Parse_MultipleConditionsAndActions_ParsesAll()
    {
        var dsl = """
            POLICY "complex-policy"
            NAME "Complex Policy"
            VERSION 2
            DOMAIN "identity"
            WHEN trust_score less_than "50"
            WHEN status equals "pending"
            THEN deny reason="Not trusted"
            THEN log level="warning"
            """;

        var result = _engine.Parse(dsl);

        Assert.Equal(2, result.Conditions.Count);
        Assert.Equal(2, result.Actions.Count);
        Assert.Equal("status", result.Conditions[1].Field);
        Assert.Equal("log", result.Actions[1].ActionType);
    }

    [Fact]
    public void Parse_WithComments_IgnoresComments()
    {
        var dsl = """
            # This is a comment
            POLICY "test-policy"
            NAME "Test Policy"
            # Another comment
            VERSION 1
            DOMAIN "identity"
            WHEN status equals "active"
            THEN allow
            """;

        var result = _engine.Parse(dsl);

        Assert.Equal("test-policy", result.PolicyId);
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _engine.Parse(""));
    }

    [Fact]
    public void Parse_WhitespaceOnly_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _engine.Parse("   \n  \n  "));
    }

    [Fact]
    public void Parse_MissingPolicyId_ThrowsArgumentException()
    {
        var dsl = """
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("POLICY", ex.Message);
    }

    [Fact]
    public void Parse_MissingName_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            VERSION 1
            DOMAIN "identity"
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("NAME", ex.Message);
    }

    [Fact]
    public void Parse_MissingVersion_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            DOMAIN "identity"
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("VERSION", ex.Message);
    }

    [Fact]
    public void Parse_MissingDomain_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("DOMAIN", ex.Message);
    }

    [Fact]
    public void Parse_MissingConditions_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("WHEN", ex.Message);
    }

    [Fact]
    public void Parse_MissingActions_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            WHEN status equals "active"
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("THEN", ex.Message);
    }

    [Fact]
    public void Parse_UnknownAction_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            WHEN status equals "active"
            THEN explode
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("Unknown action", ex.Message);
    }

    [Fact]
    public void Parse_UnknownOperator_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            WHEN status magic "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("Unknown operator", ex.Message);
    }

    [Fact]
    public void Parse_UnknownDirective_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION 1
            DOMAIN "identity"
            FOOBAR something
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("Unknown directive", ex.Message);
    }

    [Fact]
    public void Parse_InvalidVersion_ThrowsArgumentException()
    {
        var dsl = """
            POLICY "test"
            NAME "Test"
            VERSION abc
            DOMAIN "identity"
            WHEN status equals "active"
            THEN allow
            """;

        var ex = Assert.Throws<ArgumentException>(() => _engine.Parse(dsl));
        Assert.Contains("VERSION", ex.Message);
    }

    [Fact]
    public void Parse_ActionWithMultipleParameters_ParsesAll()
    {
        var dsl = """
            POLICY "notify-policy"
            NAME "Notify Policy"
            VERSION 1
            DOMAIN "identity"
            WHEN trust_score less_than "30"
            THEN notify channel="slack" message="Low trust" severity="high"
            """;

        var result = _engine.Parse(dsl);

        var action = result.Actions[0];
        Assert.Equal("notify", action.ActionType);
        Assert.Equal(3, action.Parameters.Count);
        Assert.Equal("slack", action.Parameters["channel"]);
        Assert.Equal("Low trust", action.Parameters["message"]);
        Assert.Equal("high", action.Parameters["severity"]);
    }

    [Fact]
    public void Parse_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var result = _engine.Parse(ValidDsl);
        var after = DateTime.UtcNow;

        Assert.InRange(result.CreatedAt, before, after);
    }
}
