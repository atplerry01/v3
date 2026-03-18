namespace Whycespace.Domain.Core.Cluster.Aggregates;

public sealed record ClusterAdministration(
    Guid AdministrationId,
    Guid ClusterId,
    Guid OperatorId,
    string Operation,
    string Reason,
    DateTimeOffset ExecutedAt
);
