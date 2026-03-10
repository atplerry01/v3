namespace Whycespace.Domain.Application.Commands;

using Whycespace.Contracts.Commands;

public sealed record AllocateCapitalCommand(
    Guid CommandId,
    Guid VaultId,
    decimal Amount,
    string Purpose
) : ICommand
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
