namespace Whycespace.CommandSystem.Registry;

public sealed record CommandDescriptor(
    string CommandId,
    string Domain,
    CommandVersion Version,
    Type CommandType,
    string? Description,
    IReadOnlyList<CommandPropertyDescriptor> Properties
);

public sealed record CommandPropertyDescriptor(
    string Name,
    string TypeName,
    bool IsRequired
);
