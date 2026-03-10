namespace Whycespace.Tests.Domain;

using Whycespace.Domain.Economic;
using Whycespace.Domain.Cluster.Mobility;
using Whycespace.Domain.Cluster.Property;
using Whycespace.Domain.Spv;
using Whycespace.Shared.Location;
using Xunit;

public sealed class DomainModelTests
{
    [Fact]
    public void Vault_IsImmutable()
    {
        var vault = new Vault(Guid.NewGuid(), Guid.NewGuid(), 10000m, "GBP", DateTimeOffset.UtcNow);
        Assert.Equal(10000m, vault.Balance);
        Assert.Equal("GBP", vault.Currency);
    }

    [Fact]
    public void Ride_IsImmutable()
    {
        var ride = new Ride(
            Guid.NewGuid(), Guid.NewGuid(), null,
            new GeoLocation(51.5074, -0.1278),
            new GeoLocation(51.5155, -0.1419),
            RideStatus.Requested, null, DateTimeOffset.UtcNow, null);

        Assert.Equal(RideStatus.Requested, ride.Status);
        Assert.Null(ride.DriverId);
    }

    [Fact]
    public void PropertyListing_IsImmutable()
    {
        var listing = new PropertyListing(
            Guid.NewGuid(), Guid.NewGuid(), "2 Bed Flat", "Nice flat in London",
            new GeoLocation(51.5074, -0.1278), 1500m,
            PropertyStatus.Available, DateTimeOffset.UtcNow);

        Assert.Equal(PropertyStatus.Available, listing.Status);
        Assert.Equal(1500m, listing.MonthlyRent);
    }

    [Fact]
    public void Spv_IsImmutable()
    {
        var spv = new Spv(Guid.NewGuid(), "TestSPV", Guid.NewGuid(), 50000m, SpvStatus.Active, DateTimeOffset.UtcNow);
        Assert.Equal(SpvStatus.Active, spv.Status);
    }

    [Fact]
    public void EconomicLifecycle_VaultToDistribution()
    {
        var vaultId = Guid.NewGuid();
        var vault = new Vault(vaultId, Guid.NewGuid(), 100000m, "GBP", DateTimeOffset.UtcNow);
        var capital = new Capital(Guid.NewGuid(), vaultId, 50000m, "Investment", DateTimeOffset.UtcNow);
        var spv = new Spv(Guid.NewGuid(), "TestSPV", capital.CapitalId, 50000m, SpvStatus.Active, DateTimeOffset.UtcNow);
        var asset = new Asset(Guid.NewGuid(), spv.SpvId, "Vehicle", "Fleet car", 25000m, DateTimeOffset.UtcNow);
        var revenue = new Revenue(Guid.NewGuid(), spv.SpvId, asset.AssetId, 5000m, "Fare", DateTimeOffset.UtcNow);
        var distribution = new ProfitDistribution(Guid.NewGuid(), spv.SpvId, vaultId, 2000m, DateTimeOffset.UtcNow);

        Assert.Equal(vault.VaultId, capital.VaultId);
        Assert.Equal(capital.CapitalId, spv.CapitalId);
        Assert.Equal(spv.SpvId, asset.SpvId);
        Assert.Equal(spv.SpvId, revenue.SpvId);
        Assert.Equal(spv.SpvId, distribution.SpvId);
    }
}
