namespace Whycespace.Domain.Application.Commands;

using Whycespace.Shared.Commands;

public sealed record AllocateCapitalCommand(
    Guid CommandId,
    Guid VaultId,
    decimal Amount,
    string Purpose
) : ICommand;
