namespace Whycespace.Runtime.EngineManifest.Validation;

using System.Reflection;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;

public static class EngineManifestValidator
{
    public static void Validate(Type type)
    {
        if (!typeof(IEngine).IsAssignableFrom(type))
            throw new InvalidOperationException(
                $"Type '{type.Name}' does not implement IEngine.");

        var attribute = type.GetCustomAttribute<EngineManifestAttribute>();
        if (attribute is null)
            throw new InvalidOperationException(
                $"Type '{type.Name}' is missing [EngineManifest] attribute.");

        if (string.IsNullOrWhiteSpace(attribute.InputContract))
            throw new InvalidOperationException(
                $"Engine '{type.Name}' must declare an InputContract.");

        if (attribute.OutputEvents is null || attribute.OutputEvents.Length == 0)
            throw new InvalidOperationException(
                $"Engine '{type.Name}' must declare at least one OutputEvent.");
    }
}
