namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultCreationEngineTests
{
    private readonly VaultCreationEngine _engine = new();

    [Fact]
    public async Task ValidVault_CreatesSuccessfully()
    {
        var ownerId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Investment Vault",
                ["ownerId"] = ownerId,
                ["currency"] = "GBP",
                ["vaultPurpose"] = "InvestmentCapital",
                ["description"] = "Test vault for investments",
                ["initialBalance"] = 10000m
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultCreated");
        Assert.True(result.Output.ContainsKey("vaultId"));
        Assert.True(result.Output.ContainsKey("vaultName"));
        Assert.Equal("Test Investment Vault", result.Output["vaultName"]);
        Assert.Equal(ownerId, result.Output["ownerId"]);
        Assert.Equal("InvestmentCapital", result.Output["vaultPurpose"]);
        Assert.Equal("Active", result.Output["vaultStatus"]);
        Assert.True(result.Output.ContainsKey("accountId"));
        Assert.True(result.Output.ContainsKey("policyStateId"));
        Assert.True(result.Output.ContainsKey("participantId"));
    }

    [Fact]
    public async Task ValidVault_RegistersInRegistry()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Registry Test Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "USD",
                ["vaultPurpose"] = "GeneralPurpose"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var vaultCreatedEvent = result.Events.First(e => e.EventType == "VaultCreated");
        Assert.Equal("whyce.economic.events", vaultCreatedEvent.Payload["topic"]);
        Assert.Equal("Active", vaultCreatedEvent.Payload["vaultStatus"]);
    }

    [Fact]
    public async Task MissingOwnerId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Vault",
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task EmptyOwnerId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Vault",
                ["ownerId"] = Guid.Empty.ToString(),
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingVaultName_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidPurpose_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["vaultPurpose"] = "InvalidPurpose"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnsupportedCurrency_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "BTC"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task NegativeBalance_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Test Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["initialBalance"] = -100m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitializationTest_AccountBalancePolicyCreated()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Initialization Test Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "EUR",
                ["vaultPurpose"] = "OperationalTreasury"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);

        // Verify account initialized
        Assert.True(result.Output.ContainsKey("accountId"));
        Assert.True(Guid.TryParse(result.Output["accountId"].ToString(), out _));

        // Verify balance initialized
        Assert.Equal(0m, result.Output["balance"]);
        Assert.Equal("EUR", result.Output["currency"]);

        // Verify policy state initialized
        Assert.True(result.Output.ContainsKey("policyStateId"));
        Assert.True(Guid.TryParse(result.Output["policyStateId"].ToString(), out _));

        // Verify owner participant created
        Assert.True(result.Output.ContainsKey("participantId"));
        Assert.True(Guid.TryParse(result.Output["participantId"].ToString(), out _));

        // Verify event contains initialization data
        var evt = result.Events.First(e => e.EventType == "VaultCreated");
        Assert.Equal("Compliant", evt.Payload["policyStatus"]);
        Assert.Equal("Low", evt.Payload["riskLevel"]);
    }

    [Fact]
    public async Task ClusterMetadata_IncludedInEvent()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "SPV Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["vaultPurpose"] = "SPVCapital",
                ["cluster"] = "Mobility",
                ["subCluster"] = "Taxi",
                ["spv"] = "SPV-001"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var evt = result.Events.First(e => e.EventType == "VaultCreated");
        Assert.Equal("Mobility", evt.Payload["cluster"]);
        Assert.Equal("Taxi", evt.Payload["subCluster"]);
        Assert.Equal("SPV-001", evt.Payload["spv"]);
    }

    [Fact]
    public async Task DefaultPurpose_UsesGeneralPurpose()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultName"] = "Default Purpose Vault",
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("GeneralPurpose", result.Output["vaultPurpose"]);
    }
}
