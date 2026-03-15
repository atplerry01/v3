using System.Reflection;

namespace Whycespace.GovernanceGuardian.Tests;

public class GuardianRegistryArchitectureTests
{
    private readonly Assembly _systemAssembly =
        typeof(Whycespace.System.Upstream.Governance.Registry.GuardianRegistry).Assembly;

    [Fact]
    public void SystemAssembly_DoesNotReference_EngineAssemblies()
    {
        var references = _systemAssembly.GetReferencedAssemblies();
        var engineReferences = references
            .Where(r => r.Name != null && r.Name.Contains("Engines", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(engineReferences);
    }

    [Fact]
    public void SystemAssembly_DoesNotReference_RuntimeAssemblies()
    {
        var references = _systemAssembly.GetReferencedAssemblies();
        var runtimeReferences = references
            .Where(r => r.Name != null
                && r.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase)
                && !r.Name.StartsWith("System.Runtime", StringComparison.OrdinalIgnoreCase)
                && !r.Name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(runtimeReferences);
    }
}
