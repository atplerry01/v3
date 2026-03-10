namespace Whycespace.CommandSystem.Idempotency;

public interface IIdempotencyRegistry
{
    bool Exists(Guid commandId);
    void Register(Guid commandId);
}
