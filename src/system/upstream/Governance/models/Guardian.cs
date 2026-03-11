namespace Whycespace.System.Upstream.Governance.Models;

public sealed record Guardian(
    string GuardianId,
    Guid IdentityId,
    string Name,
    GuardianStatus Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? ActivatedAt);
