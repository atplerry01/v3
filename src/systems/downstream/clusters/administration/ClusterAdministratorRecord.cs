namespace Whycespace.Systems.Downstream.Clusters.Administration;

public sealed record ClusterAdministratorRecord(
    Guid AdministratorId,
    Guid IdentityId,
    string ClusterId,
    string Role,
    string Status,
    DateTimeOffset AssignedAt,
    string? Permissions = null
);
