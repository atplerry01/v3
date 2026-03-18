namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record ListPropertyRequest(
    [Required] string PropertyId,
    [Required][StringLength(500, MinimumLength = 1)] string Address,
    [Required][Range(0.01, double.MaxValue)] decimal AskingPrice,
    [Required][StringLength(3, MinimumLength = 3)] string Currency,
    string? PropertyType);
