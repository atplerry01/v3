
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;

namespace Whycespace.EventFabric.Publisher;

public interface IEventPublisher
{
    Task PublishAsync(
        string topic,
        EventEnvelope envelope,
        CancellationToken cancellationToken
    );
}
