namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Economic;
using Xunit;

public sealed class VaultPolicyStateTests
{
    // --- PolicyStateCreationTest ---

    [Fact]
    public void Create_ValidVaultId_CreatesPolicyState()
    {
        var vaultId = Guid.NewGuid();

        var state = VaultPolicyState.Create(vaultId);

        Assert.NotEqual(Guid.Empty, state.PolicyStateId);
        Assert.Equal(vaultId, state.VaultId);
        Assert.Equal(VaultPolicyStatus.Compliant, state.PolicyStatus);
        Assert.Equal(VaultRiskLevel.Low, state.RiskLevel);
        Assert.False(state.WithdrawalRestricted);
        Assert.False(state.TransferRestricted);
        Assert.False(state.ContributionRestricted);
        Assert.False(state.DistributionRestricted);
    }

    [Fact]
    public void Create_EmptyVaultId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => VaultPolicyState.Create(Guid.Empty));
    }

    // --- PolicyOperationalTest ---

    [Fact]
    public void IsOperational_Compliant_NonCritical_ReturnsTrue()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());

        Assert.True(state.IsOperational());
    }

    [Fact]
    public void IsOperational_Compliant_CriticalRisk_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.UpdateRiskLevel(VaultRiskLevel.Critical);

        Assert.False(state.IsOperational());
    }

    [Fact]
    public void IsOperational_Restricted_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyPolicyEvaluation(VaultPolicyStatus.Restricted, VaultRiskLevel.Low);

        Assert.False(state.IsOperational());
    }

    // --- RestrictionEvaluationTest ---

    [Fact]
    public void IsWithdrawalAllowed_NotRestricted_Compliant_ReturnsTrue()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());

        Assert.True(state.IsWithdrawalAllowed());
    }

    [Fact]
    public void IsWithdrawalAllowed_Restricted_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyRestriction(withdrawalRestricted: true, transferRestricted: false,
            contributionRestricted: false, distributionRestricted: false);

        Assert.False(state.IsWithdrawalAllowed());
    }

    [Fact]
    public void IsTransferAllowed_Restricted_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyRestriction(withdrawalRestricted: false, transferRestricted: true,
            contributionRestricted: false, distributionRestricted: false);

        Assert.False(state.IsTransferAllowed());
    }

    [Fact]
    public void IsContributionAllowed_Restricted_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyRestriction(withdrawalRestricted: false, transferRestricted: false,
            contributionRestricted: true, distributionRestricted: false);

        Assert.False(state.IsContributionAllowed());
    }

    [Fact]
    public void IsDistributionAllowed_Restricted_ReturnsFalse()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyRestriction(withdrawalRestricted: false, transferRestricted: false,
            contributionRestricted: false, distributionRestricted: true);

        Assert.False(state.IsDistributionAllowed());
    }

    [Fact]
    public void Operations_NotAllowed_WhenPolicyNotCompliant()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyPolicyEvaluation(VaultPolicyStatus.UnderReview, VaultRiskLevel.Low);

        Assert.False(state.IsWithdrawalAllowed());
        Assert.False(state.IsTransferAllowed());
        Assert.False(state.IsContributionAllowed());
        Assert.False(state.IsDistributionAllowed());
    }

    // --- RiskLevelTest ---

    [Fact]
    public void CriticalRisk_MakesVaultNonOperational()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.UpdateRiskLevel(VaultRiskLevel.Critical);

        Assert.False(state.IsOperational());
    }

    [Fact]
    public void HighRisk_VaultStaysOperational_WhenCompliant()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.UpdateRiskLevel(VaultRiskLevel.High);

        Assert.True(state.IsOperational());
    }

    // --- Suspended policy restricts all operations ---

    [Fact]
    public void Suspended_RestrictsAllOperations()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.ApplyPolicyEvaluation(VaultPolicyStatus.Suspended, VaultRiskLevel.High);

        Assert.True(state.WithdrawalRestricted);
        Assert.True(state.TransferRestricted);
        Assert.True(state.ContributionRestricted);
        Assert.True(state.DistributionRestricted);
        Assert.False(state.IsWithdrawalAllowed());
        Assert.False(state.IsTransferAllowed());
        Assert.False(state.IsContributionAllowed());
        Assert.False(state.IsDistributionAllowed());
        Assert.False(state.IsOperational());
    }

    // --- Metadata ---

    [Fact]
    public void SetMetadata_StoresValues()
    {
        var state = VaultPolicyState.Create(Guid.NewGuid());
        state.SetMetadata("POL-001", "HMRC-Compliant", "Cluster-A");

        Assert.Equal("POL-001", state.PolicyReferenceId);
        Assert.Equal("HMRC-Compliant", state.ComplianceTag);
        Assert.Equal("Cluster-A", state.GovernanceScope);
    }
}
