namespace Whycespace.Contracts.Errors;

public readonly record struct ErrorCode(string Value)
{
    public static ErrorCode Unknown => new("UNKNOWN");
    public static ErrorCode ValidationFailed => new("VALIDATION_FAILED");
    public static ErrorCode NotFound => new("NOT_FOUND");
    public static ErrorCode Unauthorized => new("UNAUTHORIZED");
    public static ErrorCode Conflict => new("CONFLICT");
    public static ErrorCode Timeout => new("TIMEOUT");
    public static ErrorCode InternalError => new("INTERNAL_ERROR");

    public static implicit operator string(ErrorCode code) => code.Value;
    public static implicit operator ErrorCode(string value) => new(value);

    public override string ToString() => Value;
}
