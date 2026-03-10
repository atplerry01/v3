namespace Whycespace.Contracts.Commands;

public interface ICommand
{
    Guid CommandId { get; }
    DateTimeOffset Timestamp { get; }
}
