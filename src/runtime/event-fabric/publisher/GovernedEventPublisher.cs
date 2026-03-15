using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Registry;
using Whycespace.EventFabric.Validation;

namespace Whycespace.EventFabric.Publisher;

public sealed class GovernedEventPublisher : IEventPublisher
{
    private readonly IEventPublisher _inner;
    private readonly EventRegistry _eventRegistry;
    private readonly SchemaValidator _schemaValidator;

    public GovernedEventPublisher(
        IEventPublisher inner,
        EventRegistry eventRegistry,
        SchemaValidator schemaValidator)
    {
        _inner = inner;
        _eventRegistry = eventRegistry;
        _schemaValidator = schemaValidator;
    }

    public async Task PublishAsync(
        string topic,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!_eventRegistry.IsRegistered(envelope.EventType))
            throw new InvalidOperationException(
                $"Event '{envelope.EventType}' is not registered. Only registered events can be published.");

        var registeredTopic = _eventRegistry.GetTopic(envelope.EventType);

        if (registeredTopic != topic)
            throw new InvalidOperationException(
                $"Event '{envelope.EventType}' is registered for topic '{registeredTopic}', not '{topic}'.");

        if (!_schemaValidator.Validate(envelope))
            throw new InvalidOperationException(
                $"Event '{envelope.EventType}' failed schema validation.");

        await _inner.PublishAsync(topic, envelope, cancellationToken);
    }
}
