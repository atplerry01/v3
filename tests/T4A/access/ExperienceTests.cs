using Whycespace.Engines.T4A.Access.Experience.Admin;
using Whycespace.Engines.T4A.Access.Experience.Investor;
using Whycespace.Engines.T4A.Access.Experience.Operator;

namespace Whycespace.T4AAccess.Tests;

public sealed class ExperienceTests
{
    private static readonly Dictionary<string, object> SampleData = new()
    {
        ["vaultId"] = "vault-1",
        ["name"] = "Test Vault",
        ["amount"] = 1000m,
        ["currency"] = "GBP",
        ["status"] = "active",
        ["internalRef"] = "ref-123",
        ["token"] = "secret-token"
    };

    [Fact]
    public void AdminShaper_IncludesAllData()
    {
        var shaper = new AdminResponseShaper();
        var shaped = shaper.Shape(SampleData) as Dictionary<string, object>;

        Assert.NotNull(shaped);
        Assert.Equal("admin", shaped!["_view"]);
        Assert.True(shaped.ContainsKey("vaultId"));
        Assert.True(shaped.ContainsKey("internalRef"));
        Assert.True(shaped.ContainsKey("token")); // Admin sees everything
    }

    [Fact]
    public void InvestorShaper_FiltersToFinancialData()
    {
        var shaper = new InvestorResponseShaper();
        var shaped = shaper.Shape(SampleData) as Dictionary<string, object>;

        Assert.NotNull(shaped);
        Assert.Equal("investor", shaped!["_view"]);
        Assert.True(shaped.ContainsKey("vaultId"));
        Assert.True(shaped.ContainsKey("amount"));
        Assert.True(shaped.ContainsKey("currency"));
        Assert.False(shaped.ContainsKey("internalRef")); // Filtered out
        Assert.False(shaped.ContainsKey("token")); // Filtered out
    }

    [Fact]
    public void OperatorShaper_ExcludesSecrets()
    {
        var shaper = new OperatorResponseShaper();
        var shaped = shaper.Shape(SampleData) as Dictionary<string, object>;

        Assert.NotNull(shaped);
        Assert.Equal("operator", shaped!["_view"]);
        Assert.True(shaped.ContainsKey("vaultId"));
        Assert.True(shaped.ContainsKey("internalRef")); // Operator can see internal refs
        Assert.False(shaped.ContainsKey("token")); // But not secrets
    }
}
