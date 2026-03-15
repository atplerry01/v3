namespace Whycespace.Domain.Core.Vault;

public sealed class VaultPolicyState
{
    public Guid PolicyStateId { get; }
    public Guid VaultId { get; }
    public VaultPolicyStatus PolicyStatus { get; private set; }
    public VaultRiskLevel RiskLevel { get; private set; }
    public bool WithdrawalRestricted { get; private set; }
    public bool TransferRestricted { get; private set; }
    public bool ContributionRestricted { get; private set; }
    public bool DistributionRestricted { get; private set; }
    public DateTime LastPolicyEvaluation { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string? PolicyReferenceId { get; private set; }
    public string? ComplianceTag { get; private set; }
    public string? GovernanceScope { get; private set; }

    private VaultPolicyState(Guid policyStateId, Guid vaultId)
    {
        PolicyStateId = policyStateId;
        VaultId = vaultId;
        PolicyStatus = VaultPolicyStatus.Compliant;
        RiskLevel = VaultRiskLevel.Low;
        WithdrawalRestricted = false;
        TransferRestricted = false;
        ContributionRestricted = false;
        DistributionRestricted = false;
        LastPolicyEvaluation = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static VaultPolicyState Create(Guid vaultId)
    {
        if (vaultId == Guid.Empty)
            throw new InvalidOperationException("VaultId must be specified.");

        return new VaultPolicyState(Guid.NewGuid(), vaultId);
    }

    public bool IsOperational()
        => PolicyStatus == VaultPolicyStatus.Compliant && RiskLevel != VaultRiskLevel.Critical;

    public bool IsWithdrawalAllowed()
        => !WithdrawalRestricted && PolicyStatus == VaultPolicyStatus.Compliant;

    public bool IsTransferAllowed()
        => !TransferRestricted && PolicyStatus == VaultPolicyStatus.Compliant;

    public bool IsContributionAllowed()
        => !ContributionRestricted && PolicyStatus == VaultPolicyStatus.Compliant;

    public bool IsDistributionAllowed()
        => !DistributionRestricted && PolicyStatus == VaultPolicyStatus.Compliant;

    public void ApplyPolicyEvaluation(VaultPolicyStatus status, VaultRiskLevel riskLevel)
    {
        PolicyStatus = status;
        RiskLevel = riskLevel;

        if (status == VaultPolicyStatus.Suspended)
        {
            WithdrawalRestricted = true;
            TransferRestricted = true;
            ContributionRestricted = true;
            DistributionRestricted = true;
        }

        LastPolicyEvaluation = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyRestriction(
        bool withdrawalRestricted,
        bool transferRestricted,
        bool contributionRestricted,
        bool distributionRestricted)
    {
        WithdrawalRestricted = withdrawalRestricted;
        TransferRestricted = transferRestricted;
        ContributionRestricted = contributionRestricted;
        DistributionRestricted = distributionRestricted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRiskLevel(VaultRiskLevel riskLevel)
    {
        RiskLevel = riskLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(string? policyReferenceId, string? complianceTag, string? governanceScope)
    {
        PolicyReferenceId = policyReferenceId;
        ComplianceTag = complianceTag;
        GovernanceScope = governanceScope;
        UpdatedAt = DateTime.UtcNow;
    }
}
