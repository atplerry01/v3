
namespace Whycespace.EventFabricRuntime.Publishing;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;

public sealed class EventSerializer
{
    public string Serialize(EventEnvelope envelope)
    {
        return global::System.Text.Json.JsonSerializer.Serialize(envelope);
    }
}
