using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityAccessScopeEngineTests
{
    private readonly IdentityAccessScopeStore _store;
    private readonly IdentityAccessScopeEngine _engine;

    public IdentityAccessScopeEngineTests()
    {
        _store = new IdentityAccessScopeStore();
        _engine = new IdentityAccessScopeEngine(_store);
    }

    [Fact]
    public void AssignScope_ShouldSucceed()
    {
        _engine.AssignScope("Admin", "cluster:whycemobility");

        var scopes = _engine.GetScopes("Admin");
        Assert.Single(scopes);
        Assert.Contains("cluster:whycemobility", scopes);
    }

    [Fact]
    public void AssignScope_Duplicate_ShouldBeIgnored()
    {
        _engine.AssignScope("Admin", "cluster:whycemobility");
        _engine.AssignScope("Admin", "cluster:whycemobility");

        var scopes = _engine.GetScopes("Admin");
        Assert.Single(scopes);
    }

    [Fact]
    public void AssignScope_EmptyRole_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignScope("", "cluster:whycemobility"));
    }

    [Fact]
    public void AssignScope_WhitespaceRole_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignScope("  ", "cluster:whycemobility"));
    }

    [Fact]
    public void AssignScope_EmptyScope_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignScope("Admin", ""));
    }

    [Fact]
    public void AssignScope_InvalidFormat_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignScope("Admin", "invalidscope"));
    }

    [Fact]
    public void GetScopes_UnknownRole_ShouldReturnEmpty()
    {
        var scopes = _engine.GetScopes("NonExistent");

        Assert.Empty(scopes);
    }

    [Fact]
    public void AssignMultipleScopes_ShouldBeRetrievable()
    {
        _engine.AssignScope("Admin", "cluster:whycemobility");
        _engine.AssignScope("Admin", "cluster:whyceproperty");
        _engine.AssignScope("Admin", "system:global");

        var scopes = _engine.GetScopes("Admin");
        Assert.Equal(3, scopes.Count);
        Assert.Contains("cluster:whycemobility", scopes);
        Assert.Contains("cluster:whyceproperty", scopes);
        Assert.Contains("system:global", scopes);
    }

    [Fact]
    public void HasScope_ShouldReturnTrue_WhenAssigned()
    {
        _engine.AssignScope("Operator", "spv:taxi");

        Assert.True(_engine.HasScope("Operator", "spv:taxi"));
    }

    [Fact]
    public void HasScope_ShouldReturnFalse_WhenNotAssigned()
    {
        Assert.False(_engine.HasScope("Operator", "cluster:whycemobility"));
    }

    [Fact]
    public void HasScope_ShouldReturnFalse_ForUnknownRole()
    {
        Assert.False(_engine.HasScope("NonExistent", "cluster:whycemobility"));
    }
}
