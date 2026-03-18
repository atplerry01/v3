namespace Whycespace.CommandSystem.Core.Idempotency;

public interface IIdempotencyRegistry
{
    bool Exists(Guid commandId);
    void Register(Guid commandId);
}
