namespace Whycespace.Runtime.EngineRegistry;

using System.Reflection;
using Whycespace.Contracts.Engines;

public static class EngineRegistryValidator
{
    public static void ValidateEngineType(Type type)
    {
        if (!typeof(IEngine).IsAssignableFrom(type))
            throw new EngineRegistryException(
                $"Type '{type.FullName}' does not implement IEngine.");

        var attribute = type.GetCustomAttribute<EngineManifestAttribute>();
        if (attribute is null)
            throw new EngineRegistryException(
                $"Type '{type.FullName}' is missing [EngineManifest] attribute.");

        if (string.IsNullOrWhiteSpace(attribute.EngineId))
            throw new EngineRegistryException(
                $"Type '{type.FullName}' has an empty EngineId.");

        if (attribute.CommandType is null)
            throw new EngineRegistryException(
                $"Engine '{attribute.EngineId}' has a null CommandType.");

        if (attribute.ResultType is null)
            throw new EngineRegistryException(
                $"Engine '{attribute.EngineId}' has a null ResultType.");
    }

    public static void ValidateUniqueness(IReadOnlyList<EngineDescriptor> descriptors)
    {
        var duplicateIds = descriptors
            .GroupBy(d => d.EngineId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
            throw new EngineRegistryException(
                $"Duplicate engine IDs detected: {string.Join(", ", duplicateIds)}");

        var duplicateCommands = descriptors
            .GroupBy(d => d.CommandType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.FullName)
            .ToList();

        if (duplicateCommands.Count > 0)
            throw new EngineRegistryException(
                $"Multiple engines registered for the same command type: {string.Join(", ", duplicateCommands)}");
    }
}
