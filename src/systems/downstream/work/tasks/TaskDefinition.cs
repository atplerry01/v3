namespace Whycespace.Systems.Downstream.Work.Tasks;

public sealed record TaskDefinition(
    Guid TaskId,
    string Name,
    string ClusterId,
    string SubClusterId,
    string TaskType,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string>? Parameters = null
);
