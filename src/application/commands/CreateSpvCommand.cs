namespace Whycespace.Application.Commands;

using Whycespace.Contracts.Commands;

public sealed record CreateSpvCommand(
    Guid CommandId,
    string Name,
    Guid CapitalId,
    decimal AllocatedCapital
) : ICommand
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
