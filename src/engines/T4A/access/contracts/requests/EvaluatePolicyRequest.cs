namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record EvaluatePolicyRequest(
    [Required] string PolicyId,
    [Required] string SubjectId,
    [Required] string Resource,
    [Required] string Action,
    IReadOnlyDictionary<string, string>? Context);
