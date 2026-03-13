namespace Whycespace.PartitionRuntime.Models;

using Whycespace.Contracts.Primitives;

public sealed record PartitionAssignment(
    PartitionKey PartitionKey,
    int PartitionId
);
