namespace Whycespace.Tests.Guardrails;

using Whycespace.ArchitectureGuardrails.Validation;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Events;

public sealed class EventValidatorTests
{
    private readonly EventSchemaValidator _validator = new();

    [Fact]
    public void EngineEvent_Passes_Validation()
    {
        var result = _validator.ValidateEventType(typeof(EngineEvent));
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void SystemEvent_Passes_Validation()
    {
        var result = _validator.ValidateEventType(typeof(SystemEvent));
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void SharedAssembly_EventTypes_Pass_Validation()
    {
        var assembly = typeof(EngineEvent).Assembly;
        var results = _validator.ValidateEventTypes(assembly);

        Assert.NotEmpty(results);
        foreach (var result in results)
            Assert.True(result.IsValid, $"{result.EventTypeName}: {string.Join("; ", result.Violations)}");
    }

    [Fact]
    public void EventType_Missing_EventId_Fails()
    {
        var result = _validator.ValidateEventType(typeof(BadEvent_NoEventId));
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("EventId"));
    }
}

// Test fixture — missing EventId
public sealed record BadEvent_NoEventId(
    string EventType,
    DateTimeOffset Timestamp
);
