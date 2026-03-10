namespace Whycespace.Observability.Models;

public sealed record HealthStatus(
    string Component,
    string Status,
    DateTime Timestamp
);
