namespace Whycespace.Runtime.CommandRouting;

public sealed record CommandRouteDescriptor(
    string CommandId,
    string EngineId,
    Type CommandType
);
