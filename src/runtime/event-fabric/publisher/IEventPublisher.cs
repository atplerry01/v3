using Whycespace.EventFabric.Models;

namespace Whycespace.EventFabric.Publisher;

public interface IEventPublisher
{
    Task PublishAsync(
        string topic,
        EventEnvelope envelope,
        CancellationToken cancellationToken
    );
}
