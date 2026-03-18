namespace Whycespace.Domain.Economic.Vault;

public sealed class VaultPurpose
{
    public Guid PurposeId { get; }
    public VaultPurposeType PurposeType { get; }
    public string PurposeName { get; }
    public string Description { get; }
    public bool IsRestricted { get; }
    public DateTime CreatedAt { get; }
    public string? PolicyTag { get; }
    public string? GovernanceScope { get; }

    public VaultPurpose(
        Guid purposeId,
        VaultPurposeType purposeType,
        string purposeName,
        string description,
        bool isRestricted,
        DateTime createdAt,
        string? policyTag = null,
        string? governanceScope = null)
    {
        if (purposeId == Guid.Empty)
            throw new InvalidOperationException("PurposeId must be specified.");

        if (string.IsNullOrWhiteSpace(purposeName))
            throw new InvalidOperationException("PurposeName must not be empty.");

        PurposeId = purposeId;
        PurposeType = purposeType;
        PurposeName = purposeName;
        Description = description ?? string.Empty;
        IsRestricted = isRestricted;
        CreatedAt = createdAt;
        PolicyTag = policyTag;
        GovernanceScope = governanceScope;
    }

    public bool IsRestrictedPurpose() => IsRestricted;

    public bool MatchesPurposeType(VaultPurposeType type) => PurposeType == type;

    public string GetPurposeDescription() =>
        $"{PurposeName} ({PurposeType}): {Description}";
}
