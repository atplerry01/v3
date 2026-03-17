namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Economic;
using Xunit;

public sealed class VaultPurposeTests
{
    private static VaultPurpose CreatePurpose(
        VaultPurposeType type = VaultPurposeType.GeneralPurpose,
        string name = "General Purpose",
        string description = "Unrestricted economic vault",
        bool isRestricted = false,
        string? policyTag = null,
        string? governanceScope = null)
    {
        return new VaultPurpose(
            Guid.NewGuid(),
            type,
            name,
            description,
            isRestricted,
            DateTime.UtcNow,
            policyTag,
            governanceScope);
    }

    // --- PurposeCreationTest ---

    [Fact]
    public void Create_ValidParameters_CreatesPurpose()
    {
        var purpose = CreatePurpose(
            VaultPurposeType.InvestmentCapital,
            "Investment Capital",
            "Capital contributed for investments");

        Assert.Equal(VaultPurposeType.InvestmentCapital, purpose.PurposeType);
        Assert.Equal("Investment Capital", purpose.PurposeName);
        Assert.Equal("Capital contributed for investments", purpose.Description);
        Assert.NotEqual(Guid.Empty, purpose.PurposeId);
    }

    [Fact]
    public void Create_EmptyPurposeId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new VaultPurpose(
                Guid.Empty,
                VaultPurposeType.GeneralPurpose,
                "General",
                "desc",
                false,
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_EmptyPurposeName_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new VaultPurpose(
                Guid.NewGuid(),
                VaultPurposeType.GeneralPurpose,
                "",
                "desc",
                false,
                DateTime.UtcNow));
    }

    // --- RestrictedPurposeTest ---

    [Fact]
    public void IsRestrictedPurpose_WhenRestricted_ReturnsTrue()
    {
        var purpose = CreatePurpose(
            VaultPurposeType.Escrow,
            "Escrow",
            "Restricted funds",
            isRestricted: true);

        Assert.True(purpose.IsRestrictedPurpose());
    }

    [Fact]
    public void IsRestrictedPurpose_WhenNotRestricted_ReturnsFalse()
    {
        var purpose = CreatePurpose(isRestricted: false);

        Assert.False(purpose.IsRestrictedPurpose());
    }

    // --- PurposeMatchingTest ---

    [Fact]
    public void MatchesPurposeType_SameType_ReturnsTrue()
    {
        var purpose = CreatePurpose(VaultPurposeType.SPVCapital, "SPV Capital");

        Assert.True(purpose.MatchesPurposeType(VaultPurposeType.SPVCapital));
    }

    [Fact]
    public void MatchesPurposeType_DifferentType_ReturnsFalse()
    {
        var purpose = CreatePurpose(VaultPurposeType.SPVCapital, "SPV Capital");

        Assert.False(purpose.MatchesPurposeType(VaultPurposeType.Escrow));
    }

    // --- GetPurposeDescription ---

    [Fact]
    public void GetPurposeDescription_ReturnsFormattedDescription()
    {
        var purpose = CreatePurpose(
            VaultPurposeType.OperationalTreasury,
            "Operational Treasury",
            "Operational spending vault");

        var result = purpose.GetPurposeDescription();

        Assert.Equal("Operational Treasury (OperationalTreasury): Operational spending vault", result);
    }

    // --- ImmutablePurposeTest ---

    [Fact]
    public void Purpose_PropertiesAreReadOnly()
    {
        var purpose = CreatePurpose(VaultPurposeType.GrantFunding, "Grant Funding");

        Assert.Equal(VaultPurposeType.GrantFunding, purpose.PurposeType);
        Assert.Equal("Grant Funding", purpose.PurposeName);
    }

    // --- Optional metadata ---

    [Fact]
    public void Create_WithPolicyTag_StoresTag()
    {
        var purpose = CreatePurpose(
            policyTag: "ESCROW_POLICY_001",
            governanceScope: "CWG");

        Assert.Equal("ESCROW_POLICY_001", purpose.PolicyTag);
        Assert.Equal("CWG", purpose.GovernanceScope);
    }

    [Fact]
    public void Create_WithoutOptionalMetadata_DefaultsToNull()
    {
        var purpose = CreatePurpose();

        Assert.Null(purpose.PolicyTag);
        Assert.Null(purpose.GovernanceScope);
    }
}
