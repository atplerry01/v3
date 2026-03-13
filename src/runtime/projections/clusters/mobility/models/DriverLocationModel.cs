namespace Whycespace.Projections.Clusters.Mobility.Models;

public sealed record DriverLocationModel(
    string DriverId,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);
