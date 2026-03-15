using System.Reflection;
using Whycespace.Engines.T3I.Governance;

namespace Whycespace.GovernanceAudit.Tests;

public class GovernanceAuditArchitectureTests
{
    private readonly Assembly _engineAssembly = typeof(GovernanceAuditEngine).Assembly;

    [Fact]
    public void Engine_IsInT3INamespace()
    {
        var ns = typeof(GovernanceAuditEngine).Namespace;
        Assert.NotNull(ns);
        Assert.StartsWith("Whycespace.Engines.T3I.Governance", ns);
    }

    [Fact]
    public void Engine_HasNoRuntimeDependency()
    {
        var references = _engineAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Engine_HasNoInfrastructureDependency()
    {
        var references = _engineAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
            r => r.Name != null && r.Name.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Engine_HasNoPersistenceLogic()
    {
        var types = _engineAssembly.GetTypes();

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                Assert.DoesNotContain("SaveAsync", method.Name);
                Assert.DoesNotContain("DeleteAsync", method.Name);
                Assert.DoesNotContain("InsertAsync", method.Name);
            }
        }
    }

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(GovernanceAuditEngine).IsSealed);
    }

    [Fact]
    public void Engine_IsStateless_NoInstanceFields()
    {
        var fields = typeof(GovernanceAuditEngine)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.Empty(fields);
    }

    [Fact]
    public void Engine_DoesNotReferenceOtherEngines()
    {
        var references = _engineAssembly.GetReferencedAssemblies();

        var engineReferences = references.Where(r =>
            r.Name != null
            && r.Name.Contains("Engines", StringComparison.OrdinalIgnoreCase)
            && r.Name != _engineAssembly.GetName().Name);

        Assert.Empty(engineReferences);
    }
}
