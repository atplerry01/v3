namespace Whycespace.Application.Commands;

using Whycespace.Contracts.Commands;
using Whycespace.Shared.Location;

public sealed record ListPropertyCommand(
    Guid CommandId,
    Guid OwnerId,
    string Title,
    string Description,
    GeoLocation Location,
    decimal MonthlyRent
) : ICommand
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
