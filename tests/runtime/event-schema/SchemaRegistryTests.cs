using Whycespace.EventSchema.Registry;

namespace Whycespace.EventSchema.Tests;

public class SchemaRegistryTests
{
    [Fact]
    public void RegisterSchema_And_GetLatest_Returns_Schema()
    {
        var registry = new EventSchemaRegistry();
        var schema = new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["RideId"] = "Guid"
        });

        registry.RegisterSchema(schema);

        var latest = registry.GetLatest("DriverMatchedEvent");

        Assert.NotNull(latest);
        Assert.Equal("DriverMatchedEvent", latest.EventType);
        Assert.Equal(1, latest.SchemaVersion);
    }

    [Fact]
    public void GetLatest_Returns_Most_Recent_Version()
    {
        var registry = new EventSchemaRegistry();

        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid"
        }));

        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 2, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["DriverRating"] = "Double"
        }));

        var latest = registry.GetLatest("DriverMatchedEvent");

        Assert.NotNull(latest);
        Assert.Equal(2, latest.SchemaVersion);
    }

    [Fact]
    public void GetSchema_Returns_Specific_Version()
    {
        var registry = new EventSchemaRegistry();

        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid"
        }));

        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 2, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["DriverRating"] = "Double"
        }));

        var v1 = registry.GetSchema("DriverMatchedEvent", 1);

        Assert.NotNull(v1);
        Assert.Equal(1, v1.SchemaVersion);
    }

    [Fact]
    public void GetLatest_Returns_Null_For_Unknown_EventType()
    {
        var registry = new EventSchemaRegistry();
        Assert.Null(registry.GetLatest("UnknownEvent"));
    }
}
