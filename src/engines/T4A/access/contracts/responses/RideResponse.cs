namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record RideResponse(
    string RideId,
    string PassengerId,
    string Status,
    string? DriverId,
    string? EstimatedArrival);
