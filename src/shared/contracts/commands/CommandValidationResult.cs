namespace Whycespace.Contracts.Commands;

public sealed record CommandValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors
)
{
    public static CommandValidationResult Valid()
        => new(true, Array.Empty<string>());

    public static CommandValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
