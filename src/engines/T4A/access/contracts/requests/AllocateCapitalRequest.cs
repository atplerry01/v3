namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record AllocateCapitalRequest(
    [Required] string VaultId,
    [Required] string SpvId,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required][StringLength(3, MinimumLength = 3)] string Currency,
    string? AllocationPurpose);
