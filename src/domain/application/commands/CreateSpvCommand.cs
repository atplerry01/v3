namespace Whycespace.Domain.Application.Commands;

using Whycespace.Shared.Commands;

public sealed record CreateSpvCommand(
    Guid CommandId,
    string Name,
    Guid CapitalId,
    decimal AllocatedCapital
) : ICommand;
