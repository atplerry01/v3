using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class TrustScoreEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityTrustStore _store;
    private readonly TrustScoreEngine _engine;

    public TrustScoreEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityTrustStore();
        _engine = new TrustScoreEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void Calculate_PendingIdentity_ShouldReturnBaseScore()
    {
        var identityId = RegisterIdentity();

        var result = _engine.Calculate(identityId);

        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Calculate_MissingIdentity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.Calculate(Guid.NewGuid()));
    }

    [Fact]
    public void Calculate_VerifiedIdentity_ShouldIncreaseScore()
    {
        var identityId = RegisterIdentity();
        var identity = _registry.Get(identityId);
        identity.Verify();
        _registry.Update(identity);

        var result = _engine.Calculate(identityId);

        Assert.True(result.Score >= 50);
    }

    [Fact]
    public void Calculate_ShouldSetCalculationTimestamp()
    {
        var identityId = RegisterIdentity();

        var before = DateTime.UtcNow;
        var result = _engine.Calculate(identityId);

        Assert.True(result.CalculatedAt >= before);
    }

    [Fact]
    public void Calculate_ShouldStoreScore()
    {
        var identityId = RegisterIdentity();

        _engine.Calculate(identityId);

        var stored = _engine.Get(identityId);
        Assert.NotNull(stored);
        Assert.Equal(0, stored.Score);
    }

    [Fact]
    public void Get_ShouldReturnStoredScore()
    {
        var identityId = RegisterIdentity();
        var identity = _registry.Get(identityId);
        identity.Verify();
        _registry.Update(identity);

        var calculated = _engine.Calculate(identityId);
        var retrieved = _engine.Get(identityId);

        Assert.NotNull(retrieved);
        Assert.Equal(calculated.Score, retrieved.Score);
    }

    [Fact]
    public void Get_UnknownIdentity_ShouldReturnNull()
    {
        var result = _engine.Get(Guid.NewGuid());

        Assert.Null(result);
    }
}
