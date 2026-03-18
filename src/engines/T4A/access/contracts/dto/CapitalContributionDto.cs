namespace Whycespace.Engines.T4A.Access.Contracts.Dto;

public sealed record CapitalContributionDto(
    string ContributionId,
    string VaultId,
    string ContributorId,
    decimal Amount,
    string Currency,
    DateTimeOffset ContributedAt);
