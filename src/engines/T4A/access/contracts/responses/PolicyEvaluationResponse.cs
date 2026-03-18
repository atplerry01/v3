namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record PolicyEvaluationResponse(
    string PolicyId,
    string Decision,
    string? Reason,
    IReadOnlyDictionary<string, object>? Obligations);
