namespace Whycespace.ProjectionRuntime.Projections.Clusters.Mobility.Models;

public sealed record DriverLocationModel(
    string DriverId,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);
