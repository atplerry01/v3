namespace Whycespace.Contracts.Runtime;

public sealed record RuntimeExecutionMetadata(
    string DispatcherId,
    string PipelineName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int StepsExecuted,
    bool IsSuccessful
);
