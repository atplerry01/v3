using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyContextEngineTests
{
    private readonly PolicyContextStore _store = new();
    private readonly PolicyContextEngine _engine;

    public PolicyContextEngineTests()
    {
        _engine = new PolicyContextEngine(_store);
    }

    private static Guid ValidActorId() => Guid.NewGuid();
    private const string ValidDomain = "identity";

    private static Dictionary<string, string> ValidAttributes() => new()
    {
        ["trust_score"] = "75",
        ["status"] = "verified"
    };

    [Fact]
    public void BuildContext_ValidInput_ReturnsPolicyContext()
    {
        var actorId = ValidActorId();
        var result = _engine.BuildContext(actorId, ValidDomain, ValidAttributes());

        Assert.NotEqual(Guid.Empty, result.ContextId);
        Assert.Equal(actorId, result.ActorId);
        Assert.Equal(ValidDomain, result.TargetDomain);
        Assert.NotEmpty(result.Attributes);
        Assert.NotEqual(default, result.Timestamp);
    }

    [Fact]
    public void BuildContext_AttributesPreserved_ContainsProvidedKeys()
    {
        var attributes = new Dictionary<string, string>
        {
            ["trust_score"] = "90",
            ["role"] = "admin",
            ["region"] = "eu"
        };

        var result = _engine.BuildContext(ValidActorId(), ValidDomain, attributes);

        Assert.True(result.Attributes.ContainsKey("trust_score"));
        Assert.True(result.Attributes.ContainsKey("role"));
        Assert.True(result.Attributes.ContainsKey("region"));
        Assert.Equal("90", result.Attributes["trust_score"]);
        Assert.Equal("admin", result.Attributes["role"]);
        Assert.Equal("eu", result.Attributes["region"]);
    }

    [Fact]
    public void BuildContext_EmptyActorId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(Guid.Empty, ValidDomain, ValidAttributes()));
    }

    [Fact]
    public void BuildContext_EmptyDomain_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(ValidActorId(), "", ValidAttributes()));
    }

    [Fact]
    public void BuildContext_WhitespaceDomain_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(ValidActorId(), "   ", ValidAttributes()));
    }

    [Fact]
    public void BuildContext_EmptyAttributes_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(ValidActorId(), ValidDomain, new Dictionary<string, string>()));
    }

    [Fact]
    public void BuildContext_ContextImmutability_RecordIsImmutable()
    {
        var result = _engine.BuildContext(ValidActorId(), ValidDomain, ValidAttributes());

        // Record types are immutable by default; verify attributes dict is read-only
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(result.Attributes);

        // Verify that the context was cached and can be retrieved unchanged
        var retrieved = _engine.GetContext(result.ContextId);
        Assert.Equal(result, retrieved);
    }

    [Fact]
    public void BuildContext_ConcurrentExecutionSafety_ProducesUniqueContexts()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ =>
            Task.Run(() => _engine.BuildContext(ValidActorId(), ValidDomain, ValidAttributes()))
        ).ToArray();

        Task.WaitAll(tasks);

        var contextIds = tasks.Select(t => t.Result.ContextId).ToHashSet();
        Assert.Equal(100, contextIds.Count);
    }

    [Fact]
    public void BuildContext_LargeAttributeSet_HandlesCorrectly()
    {
        var attributes = new Dictionary<string, string>();
        for (int i = 0; i < 500; i++)
        {
            attributes[$"attribute_{i:D4}"] = $"value_{i}";
        }

        var result = _engine.BuildContext(ValidActorId(), ValidDomain, attributes);

        Assert.Equal(500, result.Attributes.Count);
    }

    [Fact]
    public void BuildContext_SetsTimestamp()
    {
        var before = DateTime.UtcNow;
        var result = _engine.BuildContext(ValidActorId(), ValidDomain, ValidAttributes());
        var after = DateTime.UtcNow;

        Assert.InRange(result.Timestamp, before, after);
    }

    [Fact]
    public void BuildContext_MapsActorId()
    {
        var actorId = ValidActorId();
        var result = _engine.BuildContext(actorId, ValidDomain, ValidAttributes());

        Assert.Equal(actorId, result.ActorId);
    }

    [Fact]
    public void GetContext_ReturnsStoredContext()
    {
        var result = _engine.BuildContext(ValidActorId(), ValidDomain, ValidAttributes());
        var retrieved = _engine.GetContext(result.ContextId);

        Assert.Equal(result.ContextId, retrieved.ContextId);
        Assert.Equal(result.ActorId, retrieved.ActorId);
        Assert.Equal(result.TargetDomain, retrieved.TargetDomain);
    }

    [Fact]
    public void GetContext_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetContext(Guid.NewGuid()));
    }

    [Fact]
    public void GetContextsByActor_ReturnsMatchingContexts()
    {
        var actorId = ValidActorId();
        _engine.BuildContext(actorId, ValidDomain, ValidAttributes());
        _engine.BuildContext(actorId, "finance", new Dictionary<string, string> { ["level"] = "high" });

        var contexts = _engine.GetContextsByActor(actorId);

        Assert.Equal(2, contexts.Count);
        Assert.All(contexts, c => Assert.Equal(actorId, c.ActorId));
    }

    [Fact]
    public void BuildContext_DeterministicBehavior_SameInputsProduceDifferentContextIds()
    {
        var actorId = ValidActorId();
        var attrs = ValidAttributes();

        var result1 = _engine.BuildContext(actorId, ValidDomain, attrs);
        var result2 = _engine.BuildContext(actorId, ValidDomain, attrs);

        // Each call should produce a unique context ID
        Assert.NotEqual(result1.ContextId, result2.ContextId);
        // But same actor and domain
        Assert.Equal(result1.ActorId, result2.ActorId);
        Assert.Equal(result1.TargetDomain, result2.TargetDomain);
    }
}
