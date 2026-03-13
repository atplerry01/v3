namespace Whycespace.ArchitectureGuardrails.Architecture;

using global::System.Reflection;

public sealed record BuildValidationResult(
    bool IsValid,
    IReadOnlyList<string> Violations
);

public sealed class BuildDeterminismValidator
{
    public BuildValidationResult Validate(Assembly assembly)
    {
        var violations = new List<string>();

        // Ensure assembly has a version
        var version = assembly.GetName().Version;
        if (version is null || version == new Version(0, 0, 0, 0))
            violations.Add($"{assembly.GetName().Name}: Assembly version is not set.");

        // Ensure no dynamic code generation attributes that would break determinism
        var dynamicTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<global::System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is null
                        && t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Any(m => m.Name.Contains("DynamicInvoke") || m.Name.Contains("Emit")))
            .ToList();

        foreach (var type in dynamicTypes)
        {
            if (type.GetMethods().Any(m => m.Name == "Emit"))
                violations.Add($"{type.FullName}: Contains IL Emit methods — builds must be deterministic. Dynamic runtime modifications are not permitted.");
        }

        // Validate no ambient static mutable state in engine types
        var engineInterface = typeof(Whycespace.Contracts.Engines.IEngine);
        var engineTypes = assembly.GetTypes()
            .Where(t => engineInterface.IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .ToList();

        foreach (var engineType in engineTypes)
        {
            var mutableStaticFields = engineType
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .ToList();

            if (mutableStaticFields.Count > 0)
            {
                var fieldNames = string.Join(", ", mutableStaticFields.Select(f => f.Name));
                violations.Add($"{engineType.Name}: Has mutable static fields ({fieldNames}) — engine outputs must be deterministic.");
            }
        }

        return new BuildValidationResult(violations.Count == 0, violations);
    }
}
