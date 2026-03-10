namespace Whycespace.Contracts.Commands;

public abstract record CommandBase(
    Guid CommandId,
    DateTimeOffset Timestamp
) : ICommand;
