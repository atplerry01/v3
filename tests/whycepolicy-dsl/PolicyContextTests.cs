using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyContextTests
{
    private readonly PolicyContextStore _store = new();
    private readonly PolicyContextEngine _engine;

    public PolicyContextTests()
    {
        _engine = new PolicyContextEngine(_store);
    }

    private static Dictionary<string, string> ValidAttributes() =>
        new() { ["trust_score"] = "75", ["status"] = "verified" };

    [Fact]
    public void BuildContext_ValidInput_ReturnsPolicyContext()
    {
        var actorId = Guid.NewGuid();
        var result = _engine.BuildContext(actorId, "identity", ValidAttributes());

        Assert.NotEqual(Guid.Empty, result.ContextId);
        Assert.Equal(actorId, result.ActorId);
        Assert.Equal("identity", result.TargetDomain);
        Assert.Equal(2, result.Attributes.Count);
    }

    [Fact]
    public void GetContext_ExistingContext_ReturnsContext()
    {
        var actorId = Guid.NewGuid();
        var created = _engine.BuildContext(actorId, "identity", ValidAttributes());

        var retrieved = _engine.GetContext(created.ContextId);

        Assert.Equal(created.ContextId, retrieved.ContextId);
        Assert.Equal(actorId, retrieved.ActorId);
    }

    [Fact]
    public void GetContext_NonExistent_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetContext(Guid.NewGuid()));
    }

    [Fact]
    public void BuildContext_AttributesPreserved_CorrectValues()
    {
        var attrs = new Dictionary<string, string>
        {
            ["trust_score"] = "90",
            ["role"] = "admin",
            ["region"] = "eu"
        };

        var result = _engine.BuildContext(Guid.NewGuid(), "identity", attrs);

        Assert.Equal("90", result.Attributes["trust_score"]);
        Assert.Equal("admin", result.Attributes["role"]);
        Assert.Equal("eu", result.Attributes["region"]);
    }

    [Fact]
    public void BuildContext_EmptyActorId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(Guid.Empty, "identity", ValidAttributes()));
    }

    [Fact]
    public void BuildContext_EmptyDomain_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(Guid.NewGuid(), "", ValidAttributes()));
    }

    [Fact]
    public void BuildContext_EmptyAttributes_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.BuildContext(Guid.NewGuid(), "identity", new Dictionary<string, string>()));
    }

    [Fact]
    public void GetContextsByActor_MultipleContexts_ReturnsAll()
    {
        var actorId = Guid.NewGuid();
        _engine.BuildContext(actorId, "identity", ValidAttributes());
        _engine.BuildContext(actorId, "access", new Dictionary<string, string> { ["level"] = "high" });

        var contexts = _engine.GetContextsByActor(actorId);

        Assert.Equal(2, contexts.Count);
    }

    [Fact]
    public void BuildContext_SetsTimestamp()
    {
        var before = DateTime.UtcNow;
        var result = _engine.BuildContext(Guid.NewGuid(), "identity", ValidAttributes());
        var after = DateTime.UtcNow;

        Assert.InRange(result.Timestamp, before, after);
    }
}
