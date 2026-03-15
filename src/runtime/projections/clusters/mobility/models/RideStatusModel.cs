namespace Whycespace.Projections.Clusters.Mobility.Models;

public sealed record RideStatusModel(
    string RideId,
    string DriverId,
    string PassengerId,
    string Status
);
