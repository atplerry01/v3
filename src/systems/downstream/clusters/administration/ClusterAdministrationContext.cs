namespace Whycespace.Systems.Downstream.Clusters.Administration;

public sealed record ClusterAdministrationContext(
    string ClusterId,
    Guid AdministratorIdentityId,
    string OperationType,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string>? Metadata = null
);
