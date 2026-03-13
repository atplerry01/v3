using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyVersionTests
{
    private readonly PolicyVersionStore _store = new();
    private readonly PolicyVersionEngine _engine;

    public PolicyVersionTests()
    {
        _engine = new PolicyVersionEngine(_store);
    }

    [Fact]
    public void CreateVersion_ValidInput_ReturnsPolicyVersion()
    {
        var result = _engine.CreateVersion("test-policy", 1);

        Assert.Equal("test-policy", result.PolicyId);
        Assert.Equal(1, result.Version);
        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    [Fact]
    public void GetLatestVersion_MultipleVersions_ReturnsHighest()
    {
        _engine.CreateVersion("test-policy", 1);
        _engine.CreateVersion("test-policy", 2);
        _engine.CreateVersion("test-policy", 3);

        var latest = _engine.GetLatestVersion("test-policy");

        Assert.Equal(3, latest.Version);
    }

    [Fact]
    public void CreateVersion_DuplicateVersion_ThrowsInvalidOperationException()
    {
        _engine.CreateVersion("test-policy", 1);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateVersion("test-policy", 1));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void GetVersions_ReturnsOrderedByVersion()
    {
        _engine.CreateVersion("test-policy", 3);
        _engine.CreateVersion("test-policy", 1);
        _engine.CreateVersion("test-policy", 2);

        var versions = _engine.GetVersions("test-policy");

        Assert.Equal(3, versions.Count);
        Assert.Equal(1, versions[0].Version);
        Assert.Equal(2, versions[1].Version);
        Assert.Equal(3, versions[2].Version);
    }

    [Fact]
    public void GetLatestVersion_NoVersions_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetLatestVersion("non-existent"));
    }

    [Fact]
    public void CompareVersions_ReturnsCorrectComparison()
    {
        _engine.CreateVersion("test-policy", 1);
        _engine.CreateVersion("test-policy", 2);

        Assert.True(_engine.CompareVersions("test-policy", 1, 2) < 0);
        Assert.True(_engine.CompareVersions("test-policy", 2, 1) > 0);
        Assert.Equal(0, _engine.CompareVersions("test-policy", 1, 1));
    }

    [Fact]
    public void CreateVersion_InvalidVersion_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.CreateVersion("test-policy", 0));
    }

    [Fact]
    public void CreateVersion_EmptyPolicyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.CreateVersion("", 1));
    }
}
