namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateVaultRequest(
    [Required][StringLength(200, MinimumLength = 1)] string Name,
    [Required] string SpvId,
    [Required][StringLength(3, MinimumLength = 3)] string Currency,
    string? Description);
