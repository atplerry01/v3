namespace Whycespace.Domain.Application.Commands;

using Whycespace.Shared.Commands;
using Whycespace.Shared.Location;

public sealed record ListPropertyCommand(
    Guid CommandId,
    Guid OwnerId,
    string Title,
    string Description,
    GeoLocation Location,
    decimal MonthlyRent
) : ICommand;
