using System.Reflection;
using Whycespace.System.Upstream.Governance.Proposals.Registry;
using Whycespace.System.Upstream.Governance.Proposals.Stores;

namespace Whycespace.GovernanceProposals.Tests;

public class GovernanceProposalArchitectureTests
{
    [Fact]
    public void Registry_HasNoEngineDependency()
    {
        var assembly = typeof(GovernanceProposalRegistry).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.StartsWith("Whycespace.", StringComparison.OrdinalIgnoreCase) && r.Name.Contains("Engines", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Registry_HasNoRuntimeDependency()
    {
        var assembly = typeof(GovernanceProposalRegistry).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.StartsWith("Whycespace.", StringComparison.OrdinalIgnoreCase) && r.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Store_HasNoEngineDependency()
    {
        var assembly = typeof(GovernanceProposalStore).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.StartsWith("Whycespace.", StringComparison.OrdinalIgnoreCase) && r.Name.Contains("Engines", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Store_HasNoRuntimeDependency()
    {
        var assembly = typeof(GovernanceProposalStore).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.StartsWith("Whycespace.", StringComparison.OrdinalIgnoreCase) && r.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Registry_IsInSystemLayer()
    {
        var ns = typeof(GovernanceProposalRegistry).Namespace;
        Assert.NotNull(ns);
        Assert.StartsWith("Whycespace.System.", ns);
    }

    [Fact]
    public void Store_IsInSystemLayer()
    {
        var ns = typeof(GovernanceProposalStore).Namespace;
        Assert.NotNull(ns);
        Assert.StartsWith("Whycespace.System.", ns);
    }
}
