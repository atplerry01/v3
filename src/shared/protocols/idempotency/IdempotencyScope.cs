namespace Whycespace.Shared.Protocols.Idempotency;

public enum IdempotencyScope
{
    Global,
    PerPartition,
    PerAggregate,
    PerConsumer
}
