namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityId
{
    public Guid Value { get; }

    private IdentityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("IdentityId cannot be empty.");

        Value = value;
    }

    public static IdentityId New() => new(Guid.NewGuid());

    public static IdentityId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
