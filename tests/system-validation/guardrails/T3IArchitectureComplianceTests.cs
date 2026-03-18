using System.Reflection;
using Whycespace.Engines.T3I.Atlas.Economic.Engines;
using Whycespace.Engines.T3I.Forecasting.Economic.Engines;
using Whycespace.Engines.T3I.Monitoring.Economic.Engines;
using Whycespace.Engines.T3I.Reporting.Chain.Engines;
using Whycespace.Engines.T3I.Shared;

namespace Whycespace.Tests.Guardrails;

/// <summary>
/// T3I Intelligence layer architecture compliance tests.
/// Enforces: no T2E dependency, no DB access, no mutation,
/// no command execution, all engines stateless.
/// </summary>
public sealed class T3IArchitectureComplianceTests
{
    private static readonly Assembly[] T3IAssemblies =
    [
        typeof(CapitalBalanceEngine).Assembly,       // Atlas
        typeof(CapitalLifecycleEngine).Assembly,     // Forecasting
        typeof(CapitalValidationEngine).Assembly,    // Monitoring
        typeof(ChainAuditEngine).Assembly,           // Reporting
    ];

    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Whycespace.Engines.T2E",
        "Whycespace.Infrastructure",
        "EntityFramework",
        "Microsoft.EntityFrameworkCore",
        "Dapper",
        "Npgsql",
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient",
        "MongoDB",
    ];

    private static readonly string[] ForbiddenMethodNames =
    [
        "SaveAsync",
        "DeleteAsync",
        "InsertAsync",
        "UpdateAsync",
        "ExecuteNonQueryAsync",
        "ExecuteScalarAsync",
    ];

    // --- Collect all T3I engine types across all assemblies ---

    private static IEnumerable<Type> GetAllT3IEngineTypes()
    {
        return T3IAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract
                && t.GetInterfaces().Any(i =>
                    i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IIntelligenceEngine<,>)));
    }

    // ================================================================
    // 1. NO T2E DEPENDENCY
    // ================================================================

    [Fact]
    public void T3I_Assemblies_DoNotReference_T2E()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var references = assembly.GetReferencedAssemblies();
            var violations = references
                .Where(r => r.Name != null && r.Name.StartsWith("Whycespace.Engines.T2E", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Name!)
                .ToList();

            Assert.True(violations.Count == 0,
                $"{assembly.GetName().Name} references T2E assemblies: {string.Join(", ", violations)}");
        }
    }

    // ================================================================
    // 2. NO DATABASE ACCESS
    // ================================================================

    [Fact]
    public void T3I_Assemblies_DoNotReference_DatabasePackages()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var references = assembly.GetReferencedAssemblies();
            var violations = references
                .Where(r => r.Name != null && ForbiddenAssemblyPrefixes.Any(p =>
                    r.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .Select(r => r.Name!)
                .ToList();

            Assert.True(violations.Count == 0,
                $"{assembly.GetName().Name} references forbidden assemblies: {string.Join(", ", violations)}");
        }
    }

    [Fact]
    public void T3I_Engines_HaveNoPersistenceMethods()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static);

                foreach (var method in methods)
                {
                    foreach (var forbidden in ForbiddenMethodNames)
                    {
                        Assert.DoesNotContain(forbidden, method.Name);
                    }
                }
            }
        }
    }

    // ================================================================
    // 3. NO MUTATION — ALL ENGINES STATELESS
    // ================================================================

    [Fact]
    public void AllT3IEngines_AreSealed()
    {
        var engines = GetAllT3IEngineTypes().ToList();
        Assert.NotEmpty(engines);

        foreach (var engine in engines)
        {
            Assert.True(engine.IsSealed,
                $"{engine.FullName} must be sealed");
        }
    }

    [Fact]
    public void AllT3IEngines_HaveNoMutableInstanceFields()
    {
        var engines = GetAllT3IEngineTypes().ToList();
        Assert.NotEmpty(engines);

        foreach (var engine in engines)
        {
            var instanceFields = engine.GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.True(instanceFields.Length == 0,
                $"{engine.FullName} has mutable instance fields: " +
                $"{string.Join(", ", instanceFields.Select(f => f.Name))}");
        }
    }

    [Fact]
    public void AllT3IEngines_HaveNoMutableStaticFields()
    {
        var engines = GetAllT3IEngineTypes().ToList();
        Assert.NotEmpty(engines);

        foreach (var engine in engines)
        {
            var staticFields = engine.GetFields(
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in staticFields)
            {
                Assert.True(field.IsInitOnly || field.IsLiteral,
                    $"{engine.FullName}.{field.Name} is a mutable static field");
            }
        }
    }

    // ================================================================
    // 4. NO COMMAND EXECUTION (engines don't implement ICommandHandler)
    // ================================================================

    [Fact]
    public void AllT3IEngines_DoNotImplementCommandHandler()
    {
        var engines = GetAllT3IEngineTypes().ToList();

        foreach (var engine in engines)
        {
            var interfaces = engine.GetInterfaces();
            var hasCommandHandler = interfaces.Any(i =>
                i.Name.Contains("CommandHandler", StringComparison.OrdinalIgnoreCase)
                || i.Name.Contains("IRequestHandler", StringComparison.OrdinalIgnoreCase));

            Assert.False(hasCommandHandler,
                $"{engine.FullName} implements a command/request handler interface — T3I engines must not execute commands");
        }
    }

    // ================================================================
    // 5. NAMESPACE CORRECTNESS
    // ================================================================

    [Fact]
    public void AllT3IEngines_AreInCorrectNamespace()
    {
        var engines = GetAllT3IEngineTypes().ToList();
        Assert.NotEmpty(engines);

        foreach (var engine in engines)
        {
            Assert.NotNull(engine.Namespace);
            Assert.StartsWith("Whycespace.Engines.T3I", engine.Namespace);
        }
    }

    // ================================================================
    // 6. NO CROSS-ENGINE REFERENCES (engine isolation)
    // ================================================================

    [Fact]
    public void T3I_Assemblies_DoNotReferenceOtherEngineTiers()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var references = assembly.GetReferencedAssemblies();
            var crossTierRefs = references
                .Where(r => r.Name != null
                    && r.Name.Contains("Engines", StringComparison.OrdinalIgnoreCase)
                    && !r.Name.Contains("T3I", StringComparison.OrdinalIgnoreCase)
                    && !r.Name.EndsWith(".Shared", StringComparison.OrdinalIgnoreCase)
                    && r.Name != assembly.GetName().Name)
                .Select(r => r.Name!)
                .ToList();

            // T0U (WhycePolicy) is allowed as a governance dependency
            crossTierRefs = crossTierRefs
                .Where(r => !r.Contains("T0U", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.True(crossTierRefs.Count == 0,
                $"{assembly.GetName().Name} references non-T3I/non-T0U engine assemblies: {string.Join(", ", crossTierRefs)}");
        }
    }

    // ================================================================
    // 7. ENGINE CONTRACT CONFORMANCE
    // ================================================================

    [Fact]
    public void AllT3IEngines_ImplementIIntelligenceEngine()
    {
        var engines = GetAllT3IEngineTypes().ToList();
        Assert.NotEmpty(engines);

        foreach (var engine in engines)
        {
            var implementsContract = engine.GetInterfaces().Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IIntelligenceEngine<,>));

            Assert.True(implementsContract,
                $"{engine.FullName} does not implement IIntelligenceEngine<TInput, TOutput>");
        }
    }

    [Fact]
    public void AllT3IEngines_HaveEngineNameProperty()
    {
        var engines = GetAllT3IEngineTypes().ToList();

        foreach (var engine in engines)
        {
            var instance = Activator.CreateInstance(engine);
            Assert.NotNull(instance);

            var prop = engine.GetProperty("EngineName");
            Assert.NotNull(prop);

            var name = prop.GetValue(instance) as string;
            Assert.False(string.IsNullOrWhiteSpace(name),
                $"{engine.FullName}.EngineName must not be null or empty");
        }
    }

    // ================================================================
    // 8. NO INFRASTRUCTURE PATTERNS
    // ================================================================

    [Fact]
    public void T3I_Assemblies_DoNotReference_HttpClient()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var references = assembly.GetReferencedAssemblies();
            var httpRefs = references
                .Where(r => r.Name != null && r.Name.Contains("System.Net.Http", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // System.Net.Http may be transitively referenced — check for direct usage
            var types = assembly.GetTypes();
            var usesHttpClient = types.Any(t =>
                t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Any(f => f.FieldType.FullName?.Contains("HttpClient") == true)
                || t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Any(p => p.PropertyType.FullName?.Contains("HttpClient") == true));

            Assert.False(usesHttpClient,
                $"{assembly.GetName().Name} uses HttpClient — T3I engines must not make HTTP calls");
        }
    }

    [Fact]
    public void T3I_Assemblies_DoNotReference_FileIO()
    {
        foreach (var assembly in T3IAssemblies)
        {
            var types = assembly.GetTypes();
            var usesFileIO = types.Any(t =>
                t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Any(f => f.FieldType.FullName?.StartsWith("System.IO.File") == true
                        || f.FieldType.FullName?.StartsWith("System.IO.Stream") == true)
                || t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Any(p => p.PropertyType.FullName?.StartsWith("System.IO.File") == true
                        || p.PropertyType.FullName?.StartsWith("System.IO.Stream") == true));

            Assert.False(usesFileIO,
                $"{assembly.GetName().Name} uses File I/O types — T3I engines must not perform file operations");
        }
    }

    // ================================================================
    // 9. ENGINE DISCOVERY — validates we actually find engines
    // ================================================================

    [Fact]
    public void T3I_EngineDiscovery_FindsEnginesAcrossAllAssemblies()
    {
        var engines = GetAllT3IEngineTypes().ToList();

        // We expect engines in atlas, forecasting, monitoring, and reporting
        Assert.True(engines.Count >= 4,
            $"Expected at least 4 T3I engines across assemblies, found {engines.Count}");

        var namespaces = engines.Select(e => e.Namespace).Distinct().ToList();
        Assert.Contains(namespaces, ns => ns!.Contains("Atlas"));
        Assert.Contains(namespaces, ns => ns!.Contains("Forecasting"));
        Assert.Contains(namespaces, ns => ns!.Contains("Monitoring"));
        Assert.Contains(namespaces, ns => ns!.Contains("Reporting"));
    }
}
