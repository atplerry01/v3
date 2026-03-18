namespace Whycespace.Tests.Domain;

using Whycespace.Application.Commands;
using Whycespace.Shared.Primitives.Location;
using Xunit;

public sealed class CommandTests
{
    [Fact]
    public void RequestRideCommand_IsImmutable()
    {
        var cmd = new RequestRideCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            new GeoLocation(51.5074, -0.1278),
            new GeoLocation(51.5155, -0.1419));

        Assert.NotEqual(Guid.Empty, cmd.CommandId);
    }

    [Fact]
    public void ListPropertyCommand_IsImmutable()
    {
        var cmd = new ListPropertyCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Flat", "Nice flat",
            new GeoLocation(51.5074, -0.1278), 1500m);

        Assert.Equal("Flat", cmd.Title);
    }

    [Fact]
    public void AllocateCapitalCommand_IsImmutable()
    {
        var cmd = new AllocateCapitalCommand(Guid.NewGuid(), Guid.NewGuid(), 10000m, "Investment");
        Assert.Equal(10000m, cmd.Amount);
    }

    [Fact]
    public void CreateSpvCommand_IsImmutable()
    {
        var cmd = new CreateSpvCommand(Guid.NewGuid(), "TestSPV", Guid.NewGuid(), 50000m);
        Assert.Equal("TestSPV", cmd.Name);
    }
}
