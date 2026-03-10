namespace Whycespace.Domain.Cluster;

public sealed record ClusterAdministration(
    Guid AdministrationId,
    Guid ClusterId,
    Guid OperatorId,
    string Operation,
    string Reason,
    DateTimeOffset ExecutedAt
);
