namespace Whycespace.ArchitectureGuardrails.Validation;

using global::System.Reflection;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.Contracts.Engines;

public sealed record EngineValidationResult(
    string EngineName,
    bool IsValid,
    IReadOnlyList<string> Violations
);

public sealed class EngineArchitectureValidator
{
    public EngineValidationResult ValidateEngine(Type engineType)
    {
        var violations = new List<string>();
        var name = engineType.Name;

        if (!typeof(IEngine).IsAssignableFrom(engineType))
            violations.Add($"{name}: Must implement IEngine.");

        if (!engineType.IsSealed)
            violations.Add($"{name}: Must be a sealed class. [{ArchitectureRules.StatelessEngines}]");

        if (engineType.IsAbstract)
            violations.Add($"{name}: Must not be abstract.");

        // Stateless check: no mutable instance fields
        var instanceFields = engineType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => !f.IsInitOnly && !f.IsLiteral)
            .ToList();

        if (instanceFields.Count > 0)
        {
            var fieldNames = string.Join(", ", instanceFields.Select(f => f.Name));
            violations.Add($"{name}: Has mutable instance fields ({fieldNames}). [{ArchitectureRules.StatelessEngines}]");
        }

        // No engine-to-engine direct calls: check if constructor takes IEngine dependencies
        var constructors = engineType.GetConstructors();
        foreach (var ctor in constructors)
        {
            var engineParams = ctor.GetParameters()
                .Where(p => typeof(IEngine).IsAssignableFrom(p.ParameterType))
                .ToList();

            if (engineParams.Count > 0)
            {
                violations.Add($"{name}: Constructor accepts IEngine parameters — engines must not reference other engines. [{ArchitectureRules.NoEngineToEngineCalls}]");
            }
        }

        // Check fields/properties that hold IEngine references
        var engineFields = engineType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Where(f => typeof(IEngine).IsAssignableFrom(f.FieldType))
            .ToList();

        if (engineFields.Count > 0)
        {
            violations.Add($"{name}: Holds IEngine field references. [{ArchitectureRules.NoEngineToEngineCalls}]");
        }

        return new EngineValidationResult(name, violations.Count == 0, violations);
    }

    public IReadOnlyList<EngineValidationResult> ValidateAllEngines(Assembly engineAssembly)
    {
        var engineTypes = engineAssembly.GetTypes()
            .Where(t => typeof(IEngine).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .ToList();

        return engineTypes.Select(ValidateEngine).ToList();
    }

    public bool ValidateEngine(Type engineType, out IReadOnlyList<string> violations)
    {
        var result = ValidateEngine(engineType);
        violations = result.Violations;
        return result.IsValid;
    }
}
