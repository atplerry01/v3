namespace Whycespace.Contracts.Policy;

public sealed record PolicyContext(
    string SubjectId,
    string ResourceId,
    string Action,
    IReadOnlyDictionary<string, object> Attributes
);
