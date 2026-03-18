namespace Whycespace.Contracts.Errors;

public class WhycespaceException : Exception
{
    public ErrorCode ErrorCode { get; }
    public IReadOnlyList<ErrorDetail> Details { get; }

    public WhycespaceException(ErrorCode errorCode, string message, IReadOnlyList<ErrorDetail>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? Array.Empty<ErrorDetail>();
    }

    public WhycespaceException(ErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = Array.Empty<ErrorDetail>();
    }
}
