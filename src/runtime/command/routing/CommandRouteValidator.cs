namespace Whycespace.CommandSystem.Routing;

using Whycespace.Runtime.EngineRegistry;

public static class CommandRouteValidator
{
    public static void ValidateNoDuplicates(IReadOnlyList<CommandRouteDescriptor> routes)
    {
        var duplicates = routes
            .GroupBy(r => r.CommandId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new CommandRoutingException(
                $"Duplicate command routes detected for: {string.Join(", ", duplicates)}.",
                duplicates.First());
        }
    }

    public static void ValidateEngineReferences(
        IReadOnlyList<CommandRouteDescriptor> routes,
        EngineRegistry engineRegistry)
    {
        var missing = routes
            .Where(r => !engineRegistry.Contains(r.EngineId))
            .Select(r => $"Command '{r.CommandId}' references unknown engine '{r.EngineId}'")
            .ToList();

        if (missing.Count > 0)
        {
            throw new CommandRoutingException(
                $"Engine reference validation failed: {string.Join("; ", missing)}.",
                null);
        }
    }

    public static void ValidateNoCircularRoutes(IReadOnlyList<CommandRouteDescriptor> routes)
    {
        var engineToCommands = routes
            .GroupBy(r => r.EngineId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.CommandId).ToHashSet());

        foreach (var route in routes)
        {
            if (engineToCommands.TryGetValue(route.CommandId, out var commands) &&
                commands.Contains(route.EngineId))
            {
                throw new CommandRoutingException(
                    $"Circular route detected: command '{route.CommandId}' routes to engine '{route.EngineId}' which routes back.",
                    route.CommandId);
            }
        }
    }
}
