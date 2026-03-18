using Whycespace.EventSchema.Models;
using Whycespace.EventSchema.Versioning;

namespace Whycespace.EventSchema.Tests;

public class EventSchemaTests
{
    [Fact]
    public void Schema_Stores_EventType_And_Version()
    {
        var payload = new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["RideId"] = "Guid"
        };

        var schema = new Models.EventSchema("DriverMatchedEvent", 1, payload);

        Assert.Equal("DriverMatchedEvent", schema.EventType);
        Assert.Equal(1, schema.SchemaVersion);
        Assert.Equal(2, schema.PayloadStructure.Count);
    }

    [Fact]
    public void VersionManager_Allows_Adding_Fields()
    {
        var v1 = new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["RideId"] = "Guid"
        });

        var v2 = new Models.EventSchema("DriverMatchedEvent", 2, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["RideId"] = "Guid",
            ["DriverRating"] = "Double"
        });

        var manager = new EventVersionManager();
        Assert.True(manager.ValidateCompatibility(v1, v2));
    }

    [Fact]
    public void VersionManager_Rejects_Removing_Fields()
    {
        var v1 = new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid",
            ["RideId"] = "Guid"
        });

        var v2 = new Models.EventSchema("DriverMatchedEvent", 2, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid"
        });

        var manager = new EventVersionManager();
        Assert.False(manager.ValidateCompatibility(v1, v2));
    }

    [Fact]
    public void VersionManager_Rejects_Type_Change()
    {
        var v1 = new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "Guid"
        });

        var v2 = new Models.EventSchema("DriverMatchedEvent", 2, new Dictionary<string, string>
        {
            ["DriverId"] = "String"
        });

        var manager = new EventVersionManager();
        Assert.False(manager.ValidateCompatibility(v1, v2));
    }
}
