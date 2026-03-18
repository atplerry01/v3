namespace Whycespace.EconomicRuntime.Tests;

using Whycespace.Domain.Economic.Spv;

public sealed class SpvEconomicEntityTests
{
    [Fact]
    public void CreateSpv_RegistersAndRetrievesCorrectly()
    {
        var registry = new SpvEconomicRegistry();

        var spv = registry.RegisterSpv("WhyceMobility", "Taxi");

        var retrieved = registry.GetSpv(spv.SpvId);
        Assert.NotNull(retrieved);
        Assert.Equal("WhyceMobility", retrieved.ClusterName);
        Assert.Equal("Taxi", retrieved.SubClusterName);
    }
}
