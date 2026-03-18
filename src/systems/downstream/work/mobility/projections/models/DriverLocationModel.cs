namespace Whycespace.Systems.Downstream.Work.Mobility.Projections.Models;

public sealed record DriverLocationModel(
    string DriverId,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);
