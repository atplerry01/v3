using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;

namespace Whycespace.EventReplay.Governance.Publisher;

public sealed class ReplayPublisher
{
    public const string ReplayTopic = "whyce.events.replay";

    private readonly IEventPublisher _publisher;

    public ReplayPublisher(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(ReplayTopic, envelope, cancellationToken);
    }
}
