namespace Whycespace.Contracts.Observability;

public sealed record DiagnosticContext(
    string ComponentName,
    string OperationName,
    string CorrelationId,
    IReadOnlyDictionary<string, object> Properties
);
