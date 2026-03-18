namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record RequestRideRequest(
    [Required] string PassengerId,
    [Required] double PickupLatitude,
    [Required] double PickupLongitude,
    [Required] double DropoffLatitude,
    [Required] double DropoffLongitude,
    string? VehicleType);
