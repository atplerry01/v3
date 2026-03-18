namespace Whycespace.CommandSystem.Routing;

public sealed record CommandRouteDescriptor(
    string CommandId,
    string EngineId,
    Type CommandType
);
