namespace Whycespace.EventFabricRuntime.Publishing;

using Whycespace.EventFabricRuntime.Models;

public sealed class EventSerializer
{
    public string Serialize(EventEnvelope envelope)
    {
        return global::System.Text.Json.JsonSerializer.Serialize(envelope);
    }
}
