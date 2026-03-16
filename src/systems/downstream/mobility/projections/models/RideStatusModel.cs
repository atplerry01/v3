namespace Whycespace.Systems.Downstream.Mobility.Projections.Models;

public sealed record RideStatusModel(
    string RideId,
    string DriverId,
    string PassengerId,
    string Status
);
