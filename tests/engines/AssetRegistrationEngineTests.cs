namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Core.Asset;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class AssetRegistrationEngineTests
{
    private readonly AssetRegistrationEngine _engine = new();

    [Fact]
    public async Task ValidAsset_RegistersSuccessfully()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Register",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetType"] = "Vehicle",
                ["description"] = "Fleet car",
                ["value"] = 25000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "AssetRegistered");
        Assert.True(result.Output.ContainsKey("assetId"));
    }

    [Fact]
    public async Task InvalidAssetType_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Register",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetType"] = "Crypto",
                ["description"] = "Token",
                ["value"] = 1000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingDescription_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Register",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetType"] = "Vehicle",
                ["value"] = 25000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
