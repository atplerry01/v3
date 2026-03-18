namespace Whycespace.Domain.Clusters.Operations.Shared;

public readonly record struct WorkerId(Guid Value)
{
    public static WorkerId New() => new(Guid.NewGuid());
    public static WorkerId Empty => new(Guid.Empty);
    public static implicit operator Guid(WorkerId id) => id.Value;
    public static implicit operator WorkerId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
