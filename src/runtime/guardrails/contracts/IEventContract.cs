namespace Whycespace.ArchitectureGuardrails.Contracts;

/// <summary>
/// Defines the architectural contract that all events must satisfy.
/// Events must be immutable records with EventId, EventType, and Timestamp.
/// </summary>
public interface IEventContract
{
    Guid EventId { get; }

    string EventType { get; }

    DateTimeOffset Timestamp { get; }
}
