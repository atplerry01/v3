using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyDomainBindingTests
{
    private readonly PolicyDomainBindingStore _store = new();
    private readonly PolicyDomainBindingEngine _engine;

    public PolicyDomainBindingTests()
    {
        _engine = new PolicyDomainBindingEngine(_store);
    }

    [Fact]
    public void BindPolicy_ValidBinding_Succeeds()
    {
        var binding = _engine.BindPolicy("pol-1", "1", "identity");

        Assert.Equal("pol-1", binding.PolicyId);
        Assert.Equal("1", binding.Version);
        Assert.Equal("identity", binding.Domain);
    }

    [Fact]
    public void GetDomainsForPolicy_ReturnsBoundDomains()
    {
        _engine.BindPolicy("pol-2", "1", "identity");
        _engine.BindPolicy("pol-2", "1", "clusters");

        var domains = _engine.GetDomainsForPolicy("pol-2");

        Assert.Equal(2, domains.Count);
        Assert.Contains("identity", domains);
        Assert.Contains("clusters", domains);
    }

    [Fact]
    public void GetPoliciesForDomain_ReturnsBoundPolicies()
    {
        _engine.BindPolicy("pol-3a", "1", "economic");
        _engine.BindPolicy("pol-3b", "1", "economic");

        var policies = _engine.GetPoliciesForDomain("economic");

        Assert.Equal(2, policies.Count);
        Assert.Contains(policies, p => p.PolicyId == "pol-3a");
        Assert.Contains(policies, p => p.PolicyId == "pol-3b");
    }

    [Fact]
    public void BindPolicy_MultipleDomainsPerPolicy_Supported()
    {
        _engine.BindPolicy("pol-multi", "1", "identity");
        _engine.BindPolicy("pol-multi", "1", "clusters");
        _engine.BindPolicy("pol-multi", "1", "economic");

        var domains = _engine.GetDomainsForPolicy("pol-multi");

        Assert.Equal(3, domains.Count);
    }

    [Fact]
    public void GetPoliciesForDomain_MultiplePoliciesPerDomain_Supported()
    {
        _engine.BindPolicy("pol-a", "1", "shared-domain");
        _engine.BindPolicy("pol-b", "1", "shared-domain");
        _engine.BindPolicy("pol-c", "1", "shared-domain");

        var policies = _engine.GetPoliciesForDomain("shared-domain");

        Assert.Equal(3, policies.Count);
    }

    [Fact]
    public void GetDomainsForPolicy_NoBindings_ReturnsDefaultPlatform()
    {
        var domains = _engine.GetDomainsForPolicy("unbound-policy");

        Assert.Single(domains);
        Assert.Equal("platform", domains[0]);
    }

    [Fact]
    public void BindPolicy_DuplicateBinding_Throws()
    {
        _engine.BindPolicy("pol-dup", "1", "identity");

        Assert.Throws<InvalidOperationException>(() =>
            _engine.BindPolicy("pol-dup", "1", "identity"));
    }
}
