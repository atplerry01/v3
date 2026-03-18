namespace Whycespace.Engines.T4A.Access.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record RegisterIdentityRequest(
    [Required][StringLength(200, MinimumLength = 1)] string DisplayName,
    [Required][EmailAddress] string Email,
    [Required] string IdentityType,
    string? OrganizationId);
