using Whycespace.System.Upstream.WhycePolicy.Dsl;
using Whycespace.System.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public sealed class PolicyDslTests
{
    private static PolicyDslCondition MakeCondition(string attr = "status", string op = PolicyOperator.Equals, string value = "active") =>
        new(attr, op, value);

    private static PolicyDslAction MakeAction(PolicyActionType type = PolicyActionType.Allow, string reason = "Default") =>
        new(type, reason, new Dictionary<string, string>());

    // 1. PolicyDefinition must require conditions
    [Fact]
    public void Create_EmptyConditions_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PolicyDslDefinition.Create(
                "p1", "Test", "identity",
                Array.Empty<PolicyDslCondition>(),
                new[] { MakeAction() }));

        Assert.Contains("condition", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // 2. PolicyDefinition must require actions
    [Fact]
    public void Create_EmptyActions_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PolicyDslDefinition.Create(
                "p1", "Test", "identity",
                new[] { MakeCondition() },
                Array.Empty<PolicyDslAction>()));

        Assert.Contains("action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // 3. PolicyCondition operators must be valid
    [Theory]
    [InlineData(PolicyOperator.Equals)]
    [InlineData(PolicyOperator.NotEquals)]
    [InlineData(PolicyOperator.GreaterThan)]
    [InlineData(PolicyOperator.LessThan)]
    [InlineData(PolicyOperator.Contains)]
    [InlineData(PolicyOperator.Exists)]
    public void PolicyOperator_AllValues_AreValid(string op)
    {
        Assert.True(PolicyOperator.IsValid(op));
    }

    [Fact]
    public void PolicyOperator_InvalidValue_IsNotValid()
    {
        Assert.False(PolicyOperator.IsValid("invalid_operator"));
    }

    // 4. PolicyAction types must map correctly
    [Theory]
    [InlineData(PolicyActionType.Allow, 0)]
    [InlineData(PolicyActionType.Deny, 1)]
    [InlineData(PolicyActionType.RequireApproval, 2)]
    [InlineData(PolicyActionType.RequireGuardian, 3)]
    [InlineData(PolicyActionType.RequireQuorum, 4)]
    public void PolicyActionType_Values_MapCorrectly(PolicyActionType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }

    // 5. Policy lifecycle transitions valid
    [Fact]
    public void PolicyDslDefinition_DefaultLifecycleState_IsDraft()
    {
        var policy = PolicyDslDefinition.Create(
            "p1", "Test", "identity",
            new[] { MakeCondition() },
            new[] { MakeAction() });

        Assert.Equal(PolicyLifecycleState.Draft, policy.LifecycleState);
    }

    [Theory]
    [InlineData(PolicyLifecycleState.Draft)]
    [InlineData(PolicyLifecycleState.Active)]
    [InlineData(PolicyLifecycleState.Revoked)]
    [InlineData(PolicyLifecycleState.Archived)]
    public void PolicyDslDefinition_LifecycleState_CanBeSet(PolicyLifecycleState state)
    {
        var policy = PolicyDslDefinition.Create(
            "p1", "Test", "identity",
            new[] { MakeCondition() },
            new[] { MakeAction() }) with { LifecycleState = state };

        Assert.Equal(state, policy.LifecycleState);
    }

    // 6. Priority ordering test
    [Fact]
    public void PolicyPriority_Ordering_LowToHigh()
    {
        Assert.True(PolicyPriority.Low < PolicyPriority.Medium);
        Assert.True(PolicyPriority.Medium < PolicyPriority.High);
        Assert.True(PolicyPriority.High < PolicyPriority.Critical);
    }

    [Fact]
    public void Create_ValidInput_SetsAllFields()
    {
        var conditions = new[] { MakeCondition("age", PolicyOperator.GreaterThan, "18") };
        var actions = new[] { MakeAction(PolicyActionType.Deny, "Underage") };

        var policy = PolicyDslDefinition.Create(
            "age-gate", "Age Gate Policy", "identity",
            conditions, actions,
            PolicyPriority.High, "Blocks minors");

        Assert.Equal("age-gate", policy.PolicyId);
        Assert.Equal("Age Gate Policy", policy.PolicyName);
        Assert.Equal("identity", policy.PolicyDomain);
        Assert.Equal("Blocks minors", policy.PolicyDescription);
        Assert.Equal(PolicyPriority.High, policy.Priority);
        Assert.Single(policy.Conditions);
        Assert.Single(policy.Actions);
        Assert.Equal(1, policy.Version);
        Assert.Equal(PolicyLifecycleState.Draft, policy.LifecycleState);
    }

    [Fact]
    public void PolicyDslDefinition_IsImmutable_WithExpression()
    {
        var original = PolicyDslDefinition.Create(
            "p1", "Original", "identity",
            new[] { MakeCondition() },
            new[] { MakeAction() });

        var updated = original with { PolicyName = "Updated", Version = 2 };

        Assert.Equal("Original", original.PolicyName);
        Assert.Equal(1, original.Version);
        Assert.Equal("Updated", updated.PolicyName);
        Assert.Equal(2, updated.Version);
    }
}
