using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyContextEngineTests
{
    private readonly PolicyContextStore _store = new();
    private readonly PolicyContextEngine _engine;

    public PolicyContextEngineTests()
    {
        _engine = new PolicyContextEngine(_store);
    }

    private static PolicyContextInput ValidInput() => new(
        IdentityId: Guid.NewGuid(),
        ActionType: "read",
        ResourceType: "document",
        ResourceId: "doc-001",
        ClusterId: Guid.NewGuid(),
        SubClusterId: Guid.NewGuid(),
        SpvId: Guid.NewGuid(),
        VaultId: Guid.NewGuid(),
        Attributes: new Dictionary<string, object>
        {
            ["Trust_Score"] = 75,
            ["Status"] = "verified"
        }
    );

    [Fact]
    public void BuildContext_ValidInput_ReturnsPolicyContext()
    {
        var input = ValidInput();
        var result = _engine.BuildContext(input);

        Assert.NotEqual(Guid.Empty, result.ContextId);
        Assert.Equal(input.IdentityId, result.IdentityId);
        Assert.Equal(input.ActionType, result.ActionType);
        Assert.Equal(input.ResourceType, result.ResourceType);
        Assert.Equal(input.ResourceId, result.ResourceId);
        Assert.Equal(input.ClusterId, result.ClusterId);
        Assert.Equal(input.SubClusterId, result.SubClusterId);
        Assert.Equal(input.SpvId, result.SpvId);
        Assert.Equal(input.VaultId, result.VaultId);
        Assert.NotEqual(default, result.ContextCreatedAt);
    }

    [Fact]
    public void BuildContext_AttributeNormalization_KeysAreLowercase()
    {
        var input = ValidInput() with
        {
            Attributes = new Dictionary<string, object>
            {
                ["Trust_SCORE"] = 90,
                ["ROLE"] = "admin",
                ["Region"] = "eu"
            }
        };

        var result = _engine.BuildContext(input);

        Assert.True(result.Attributes.ContainsKey("trust_score"));
        Assert.True(result.Attributes.ContainsKey("role"));
        Assert.True(result.Attributes.ContainsKey("region"));
        Assert.False(result.Attributes.ContainsKey("Trust_SCORE"));
        Assert.False(result.Attributes.ContainsKey("ROLE"));
        Assert.False(result.Attributes.ContainsKey("Region"));
    }

    [Fact]
    public void BuildContext_DeterministicAttributeOrdering_SortedByKey()
    {
        var input = ValidInput() with
        {
            Attributes = new Dictionary<string, object>
            {
                ["zebra"] = "z",
                ["alpha"] = "a",
                ["middle"] = "m"
            }
        };

        var result = _engine.BuildContext(input);
        var keys = result.Attributes.Keys.ToList();

        Assert.Equal("alpha", keys[0]);
        Assert.Equal("middle", keys[1]);
        Assert.Equal("zebra", keys[2]);
    }

    [Fact]
    public void BuildContext_MissingAttributes_AllowsEmptyAttributes()
    {
        var input = ValidInput() with
        {
            Attributes = new Dictionary<string, object>()
        };

        var result = _engine.BuildContext(input);

        Assert.Empty(result.Attributes);
    }

    [Fact]
    public void BuildContext_MissingOptionalGuids_AllowsDefaultGuids()
    {
        var input = ValidInput() with
        {
            SubClusterId = Guid.Empty,
            SpvId = Guid.Empty,
            VaultId = Guid.Empty
        };

        var result = _engine.BuildContext(input);

        Assert.Equal(Guid.Empty, result.SubClusterId);
        Assert.Equal(Guid.Empty, result.SpvId);
        Assert.Equal(Guid.Empty, result.VaultId);
    }

    [Fact]
    public void BuildContext_ContextImmutability_RecordIsImmutable()
    {
        var input = ValidInput();
        var result = _engine.BuildContext(input);

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
            Task.Run(() => _engine.BuildContext(ValidInput()))
        ).ToArray();

        Task.WaitAll(tasks);

        var contextIds = tasks.Select(t => t.Result.ContextId).ToHashSet();
        Assert.Equal(100, contextIds.Count);
    }

    [Fact]
    public void BuildContext_LargeAttributeSet_HandlesCorrectly()
    {
        var attributes = new Dictionary<string, object>();
        for (int i = 0; i < 500; i++)
        {
            attributes[$"Attribute_{i:D4}"] = $"value_{i}";
        }

        var input = ValidInput() with { Attributes = attributes };

        var result = _engine.BuildContext(input);

        Assert.Equal(500, result.Attributes.Count);
        // Verify all keys are lowercase
        Assert.All(result.Attributes.Keys, key => Assert.Equal(key, key.ToLowerInvariant()));
        // Verify ordering is deterministic
        var keys = result.Attributes.Keys.ToList();
        var sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.Equal(sorted, keys);
    }

    [Fact]
    public void BuildContext_EmptyIdentityId_ThrowsArgumentException()
    {
        var input = ValidInput() with { IdentityId = Guid.Empty };
        Assert.Throws<ArgumentException>(() => _engine.BuildContext(input));
    }

    [Fact]
    public void BuildContext_EmptyActionType_ThrowsArgumentException()
    {
        var input = ValidInput() with { ActionType = "" };
        Assert.Throws<ArgumentException>(() => _engine.BuildContext(input));
    }

    [Fact]
    public void BuildContext_EmptyResourceType_ThrowsArgumentException()
    {
        var input = ValidInput() with { ResourceType = "" };
        Assert.Throws<ArgumentException>(() => _engine.BuildContext(input));
    }

    [Fact]
    public void BuildContext_NullAttributes_AllowsNull()
    {
        var input = ValidInput() with { Attributes = null! };
        var result = _engine.BuildContext(input);
        Assert.Empty(result.Attributes);
    }

    [Fact]
    public void BuildContext_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _engine.BuildContext((PolicyContextInput)null!));
    }

    [Fact]
    public void BuildContext_AttributeValueObjectToString_ConvertsCorrectly()
    {
        var input = ValidInput() with
        {
            Attributes = new Dictionary<string, object>
            {
                ["count"] = 42,
                ["active"] = true,
                ["rate"] = 3.14
            }
        };

        var result = _engine.BuildContext(input);

        Assert.Equal("42", result.Attributes["count"]);
        Assert.Equal("True", result.Attributes["active"]);
        Assert.Equal("3.14", result.Attributes["rate"]);
    }

    [Fact]
    public void BuildContext_SetsContextCreatedAt()
    {
        var before = DateTime.UtcNow;
        var result = _engine.BuildContext(ValidInput());
        var after = DateTime.UtcNow;

        Assert.InRange(result.ContextCreatedAt, before, after);
    }

    [Fact]
    public void BuildContext_MapsActorIdFromIdentityId()
    {
        var input = ValidInput();
        var result = _engine.BuildContext(input);

        Assert.Equal(input.IdentityId, result.ActorId);
    }
}
