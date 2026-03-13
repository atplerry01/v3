using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventSchema.Registry;
using Whycespace.EventSchema.Validation;

namespace Whycespace.EventSchema.Tests;

public class SchemaValidatorTests
{
    [Fact]
    public void Validate_Returns_True_For_Valid_Event()
    {
        var registry = new EventSchemaRegistry();
        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "String",
            ["RideId"] = "String"
        }));

        var validator = new EventSchemaValidator(registry);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "DriverMatchedEvent",
            "whyce.engine.events",
            new { DriverId = "d-1", RideId = "r-1" },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        Assert.True(validator.Validate(envelope));
    }

    [Fact]
    public void Validate_Returns_False_For_Missing_Fields()
    {
        var registry = new EventSchemaRegistry();
        registry.RegisterSchema(new Models.EventSchema("DriverMatchedEvent", 1, new Dictionary<string, string>
        {
            ["DriverId"] = "String",
            ["RideId"] = "String"
        }));

        var validator = new EventSchemaValidator(registry);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "DriverMatchedEvent",
            "whyce.engine.events",
            new { DriverId = "d-1" },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        Assert.False(validator.Validate(envelope));
    }

    [Fact]
    public void Validate_Returns_False_For_Unregistered_Event()
    {
        var registry = new EventSchemaRegistry();
        var validator = new EventSchemaValidator(registry);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "UnknownEvent",
            "whyce.engine.events",
            new { },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        Assert.False(validator.Validate(envelope));
    }
}
