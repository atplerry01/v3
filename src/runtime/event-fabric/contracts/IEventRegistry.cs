namespace Whycespace.EventFabric.Contracts;

public interface IEventRegistry
{
    bool IsRegistered(string eventType);

    string? GetTopic(string eventType);

    string? GetOwningCluster(string eventType);
}
