
namespace Whycespace.EventFabricRuntime.Publishing;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EventFabricRuntime.Routing;

public sealed class EventPublisher
{
    private readonly EventSerializer _serializer;
    private readonly EventTopicRouter _router;

    public EventPublisher(EventSerializer serializer, EventTopicRouter router)
    {
        _serializer = serializer;
        _router = router;
    }

    public string Publish(EventEnvelope envelope)
    {
        var topic = _router.ResolveTopic(envelope.EventType);

        var payload = _serializer.Serialize(envelope);

        return topic + ":" + payload;
    }
}
