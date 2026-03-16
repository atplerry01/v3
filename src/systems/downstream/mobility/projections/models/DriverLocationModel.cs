namespace Whycespace.Systems.Downstream.Mobility.Projections.Models;

public sealed record DriverLocationModel(
    string DriverId,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);
