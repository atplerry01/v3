namespace Whycespace.PartitionRuntime.Models;

using Whycespace.Shared.Primitives.Common;

public sealed record PartitionAssignment(
    PartitionKey PartitionKey,
    int PartitionId
);
