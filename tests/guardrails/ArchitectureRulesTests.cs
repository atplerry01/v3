namespace Whycespace.Tests.Guardrails;

using Whycespace.ArchitectureGuardrails.Rules;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void All_Returns_Eight_Rules()
    {
        Assert.Equal(8, ArchitectureRules.All.Count);
    }

    [Fact]
    public void Names_Returns_Eight_Names()
    {
        Assert.Equal(8, ArchitectureRules.Names.Count);
    }

    [Fact]
    public void Rules_Are_Non_Empty_Strings()
    {
        foreach (var rule in ArchitectureRules.All)
            Assert.False(string.IsNullOrWhiteSpace(rule));
    }

    [Fact]
    public void StatelessEngines_Rule_Is_Defined()
    {
        Assert.Contains("stateless", ArchitectureRules.StatelessEngines, global::System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventSourcingRequired_Rule_Is_Defined()
    {
        Assert.Contains("event", ArchitectureRules.EventSourcingRequired, global::System.StringComparison.OrdinalIgnoreCase);
    }
}
