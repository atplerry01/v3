using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;
using Whycespace.EventSchema.Validation;

namespace Whycespace.EventSchema.Enforcement;

public sealed class SchemaEnforcingPublisher : IEventPublisher
{
    private readonly IEventPublisher _inner;
    private readonly EventSchemaValidator _validator;

    public SchemaEnforcingPublisher(IEventPublisher inner, EventSchemaValidator validator)
    {
        _inner = inner;
        _validator = validator;
    }

    public async Task PublishAsync(
        string topic,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!_validator.Validate(envelope))
            throw new InvalidOperationException(
                $"Event '{envelope.EventType}' failed schema validation.");

        await _inner.PublishAsync(topic, envelope, cancellationToken);
    }
}
