namespace Whycespace.Engines.T4A.Access.Contracts.Dto;

public sealed record RideStatusDto(
    string RideId,
    string PassengerId,
    string Status,
    string? DriverId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude);
