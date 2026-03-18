using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Safeguards;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class ConstitutionalSafeguardEngineTests
{
    private readonly ConstitutionalPolicyStore _store = new();
    private readonly ConstitutionalSafeguardEngine _engine;

    public ConstitutionalSafeguardEngineTests()
    {
        _engine = new ConstitutionalSafeguardEngine(_store);
    }

    [Fact]
    public void RegisterConstitutionalPolicy_ReturnsRecord()
    {
        var record = _engine.RegisterConstitutionalPolicy("pol-1", "1", "Immutable");

        Assert.Equal("pol-1", record.PolicyId);
        Assert.Equal("1", record.Version);
        Assert.Equal("Immutable", record.ProtectionLevel);
    }

    [Fact]
    public void IsProtectedPolicy_DetectsProtectedPolicy()
    {
        _engine.RegisterConstitutionalPolicy("pol-2", "1", "Immutable");

        Assert.True(_store.IsProtectedPolicy("pol-2", "1"));
        Assert.False(_store.IsProtectedPolicy("pol-unknown", "1"));
    }

    [Fact]
    public void ValidatePolicyModification_ImmutablePolicy_Throws()
    {
        _engine.RegisterConstitutionalPolicy("pol-3", "1", "Immutable");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.ValidatePolicyModification("pol-3", "1"));
        Assert.Contains("immutable", ex.Message);
        Assert.Contains("cannot be modified", ex.Message);
    }

    [Fact]
    public void ValidatePolicyDeletion_ImmutablePolicy_Throws()
    {
        _engine.RegisterConstitutionalPolicy("pol-4", "1", "Immutable");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.ValidatePolicyDeletion("pol-4", "1"));
        Assert.Contains("immutable", ex.Message);
        Assert.Contains("cannot be deleted", ex.Message);
    }

    [Fact]
    public void ValidatePolicyModification_NonProtectedPolicy_DoesNotThrow()
    {
        _engine.ValidatePolicyModification("unprotected", "1");
    }

    [Fact]
    public void GetProtectionLevel_ReturnsLevel()
    {
        _engine.RegisterConstitutionalPolicy("pol-5", "1", "SystemCritical");

        var level = _store.GetProtectionLevel("pol-5", "1");

        Assert.Equal("SystemCritical", level);
    }

    [Fact]
    public void MultipleProtectedPolicies_Supported()
    {
        _engine.RegisterConstitutionalPolicy("pol-a", "1", "Immutable");
        _engine.RegisterConstitutionalPolicy("pol-b", "1", "GuardianApprovalRequired");
        _engine.RegisterConstitutionalPolicy("pol-c", "1", "SystemCritical");

        Assert.True(_store.IsProtectedPolicy("pol-a", "1"));
        Assert.True(_store.IsProtectedPolicy("pol-b", "1"));
        Assert.True(_store.IsProtectedPolicy("pol-c", "1"));
        Assert.Equal("Immutable", _store.GetProtectionLevel("pol-a", "1"));
        Assert.Equal("GuardianApprovalRequired", _store.GetProtectionLevel("pol-b", "1"));
        Assert.Equal("SystemCritical", _store.GetProtectionLevel("pol-c", "1"));
    }
}
