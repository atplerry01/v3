namespace Whycespace.Shared.Protocols.Messaging;

public enum PartitioningStrategy
{
    ByAggregateId,
    ByTenant,
    ByCluster,
    RoundRobin
}
