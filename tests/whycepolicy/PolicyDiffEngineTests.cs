using Whycespace.Engines.T3I.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyDiffEngineTests
{
    private static PolicyDefinition MakePolicy(
        string id,
        string name = "Test Policy",
        int version = 1,
        string domain = "platform",
        IReadOnlyList<PolicyCondition>? conditions = null,
        IReadOnlyList<PolicyAction>? actions = null,
        PolicyPriority priority = PolicyPriority.Medium,
        PolicyLifecycleState lifecycle = PolicyLifecycleState.Active) =>
        new(id, name, version, domain,
            conditions ?? Array.Empty<PolicyCondition>(),
            actions ?? Array.Empty<PolicyAction>(),
            DateTime.UtcNow,
            priority,
            lifecycle);

    [Fact]
    public void GenerateDiff_MetadataChange_DetectsNameChange()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1", name: "Old Name");
        var proposed = MakePolicy("p1", name: "New Name");

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Single(result.Changes);
        Assert.Equal("Name", result.Changes[0].FieldName);
        Assert.Equal("Old Name", result.Changes[0].PreviousValue);
        Assert.Equal("New Name", result.Changes[0].ProposedValue);
        Assert.Equal(PolicyChangeType.MODIFIED, result.Changes[0].ChangeType);
        Assert.Equal("p1", result.PolicyId);
    }

    [Fact]
    public void GenerateDiff_ConditionChange_DetectsAddedAndRemovedConditions()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1",
            conditions: new[] { new PolicyCondition("role", "equals", "admin") });
        var proposed = MakePolicy("p1",
            conditions: new[] { new PolicyCondition("role", "equals", "manager") });

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Equal(2, result.Changes.Count);
        var removed = result.Changes.First(c => c.ChangeType == PolicyChangeType.REMOVED);
        var added = result.Changes.First(c => c.ChangeType == PolicyChangeType.ADDED);
        Assert.Equal("Condition", removed.FieldName);
        Assert.Equal("role:equals:admin", removed.PreviousValue);
        Assert.Equal("Condition", added.FieldName);
        Assert.Equal("role:equals:manager", added.ProposedValue);
    }

    [Fact]
    public void GenerateDiff_ActionChange_DetectsActionModification()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1",
            actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) });
        var proposed = MakePolicy("p1",
            actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) });

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Equal(2, result.Changes.Count);
        var removed = result.Changes.First(c => c.ChangeType == PolicyChangeType.REMOVED);
        var added = result.Changes.First(c => c.ChangeType == PolicyChangeType.ADDED);
        Assert.Equal("Action", removed.FieldName);
        Assert.Contains("allow", removed.PreviousValue);
        Assert.Equal("Action", added.FieldName);
        Assert.Contains("deny", added.ProposedValue);
    }

    [Fact]
    public void GenerateDiff_PriorityChange_DetectsPriorityModification()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1", priority: PolicyPriority.Low);
        var proposed = MakePolicy("p1", priority: PolicyPriority.Critical);

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Single(result.Changes);
        Assert.Equal("Priority", result.Changes[0].FieldName);
        Assert.Equal("Low", result.Changes[0].PreviousValue);
        Assert.Equal("Critical", result.Changes[0].ProposedValue);
        Assert.Equal(PolicyChangeType.MODIFIED, result.Changes[0].ChangeType);
    }

    [Fact]
    public void GenerateDiff_LifecycleChange_DetectsLifecycleModification()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1", lifecycle: PolicyLifecycleState.Draft);
        var proposed = MakePolicy("p1", lifecycle: PolicyLifecycleState.Active);

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Single(result.Changes);
        Assert.Equal("LifecycleState", result.Changes[0].FieldName);
        Assert.Equal("Draft", result.Changes[0].PreviousValue);
        Assert.Equal("Active", result.Changes[0].ProposedValue);
        Assert.Equal(PolicyChangeType.MODIFIED, result.Changes[0].ChangeType);
    }

    [Fact]
    public void GenerateDiff_DeterministicResult_SameInputProducesSameOutput()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1",
            name: "Old",
            conditions: new[] { new PolicyCondition("role", "equals", "admin") },
            actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) },
            priority: PolicyPriority.Low,
            lifecycle: PolicyLifecycleState.Draft);
        var proposed = MakePolicy("p1",
            name: "New",
            conditions: new[] { new PolicyCondition("role", "equals", "manager") },
            actions: new[] { new PolicyAction("deny", new Dictionary<string, string>()) },
            priority: PolicyPriority.High,
            lifecycle: PolicyLifecycleState.Active);

        var input = new PolicyDiffInput(previous, proposed);
        var result1 = engine.GenerateDiff(input);
        var result2 = engine.GenerateDiff(input);

        Assert.Equal(result1.ChangeCount, result2.ChangeCount);
        Assert.Equal(result1.PolicyId, result2.PolicyId);
        for (var i = 0; i < result1.Changes.Count; i++)
        {
            Assert.Equal(result1.Changes[i].FieldName, result2.Changes[i].FieldName);
            Assert.Equal(result1.Changes[i].PreviousValue, result2.Changes[i].PreviousValue);
            Assert.Equal(result1.Changes[i].ProposedValue, result2.Changes[i].ProposedValue);
            Assert.Equal(result1.Changes[i].ChangeType, result2.Changes[i].ChangeType);
        }
    }

    [Fact]
    public void GenerateDiff_NoChanges_ReturnsEmptyDiff()
    {
        var engine = new PolicyDiffEngine();
        var policy = MakePolicy("p1",
            conditions: new[] { new PolicyCondition("role", "equals", "admin") },
            actions: new[] { new PolicyAction("allow", new Dictionary<string, string>()) });

        var result = engine.GenerateDiff(new PolicyDiffInput(policy, policy));

        Assert.Empty(result.Changes);
        Assert.Equal(0, result.ChangeCount);
        Assert.Equal("p1", result.PolicyId);
    }

    [Fact]
    public void GenerateDiff_ConcurrentSafety_ProducesConsistentResults()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1",
            name: "Old",
            priority: PolicyPriority.Low,
            lifecycle: PolicyLifecycleState.Draft);
        var proposed = MakePolicy("p1",
            name: "New",
            priority: PolicyPriority.High,
            lifecycle: PolicyLifecycleState.Active);

        var input = new PolicyDiffInput(previous, proposed);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.GenerateDiff(input)))
            .ToArray();

        Task.WaitAll(tasks);

        var expected = tasks[0].Result;
        foreach (var task in tasks)
        {
            var actual = task.Result;
            Assert.Equal(expected.ChangeCount, actual.ChangeCount);
            Assert.Equal(expected.PolicyId, actual.PolicyId);
            for (var i = 0; i < expected.Changes.Count; i++)
            {
                Assert.Equal(expected.Changes[i].FieldName, actual.Changes[i].FieldName);
                Assert.Equal(expected.Changes[i].ChangeType, actual.Changes[i].ChangeType);
            }
        }
    }

    [Fact]
    public void GenerateDiff_MultipleChanges_ReturnsCorrectChangeCount()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1",
            name: "Old",
            version: 1,
            domain: "finance",
            priority: PolicyPriority.Low,
            lifecycle: PolicyLifecycleState.Draft);
        var proposed = MakePolicy("p1",
            name: "New",
            version: 2,
            domain: "hr",
            priority: PolicyPriority.High,
            lifecycle: PolicyLifecycleState.Active);

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Equal(result.Changes.Count, result.ChangeCount);
        Assert.True(result.ChangeCount >= 5);
    }

    [Fact]
    public void GenerateDiff_VersionChange_DetectsVersionModification()
    {
        var engine = new PolicyDiffEngine();
        var previous = MakePolicy("p1", version: 1);
        var proposed = MakePolicy("p1", version: 2);

        var result = engine.GenerateDiff(new PolicyDiffInput(previous, proposed));

        Assert.Single(result.Changes);
        Assert.Equal("Version", result.Changes[0].FieldName);
        Assert.Equal("1", result.Changes[0].PreviousValue);
        Assert.Equal("2", result.Changes[0].ProposedValue);
    }
}
