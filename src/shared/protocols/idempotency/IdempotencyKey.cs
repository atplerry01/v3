namespace Whycespace.Shared.Protocols.Idempotency;

public readonly record struct IdempotencyKey(string Value)
{
    public static IdempotencyKey From(string source, string operation)
        => new($"{source}:{operation}");

    public static IdempotencyKey From(Guid eventId)
        => new(eventId.ToString("N"));

    public static implicit operator string(IdempotencyKey key) => key.Value;

    public override string ToString() => Value;
}
