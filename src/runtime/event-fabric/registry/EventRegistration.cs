namespace Whycespace.EventFabric.Registry;

public sealed record EventRegistration(
    string EventType,
    int Version,
    string OwningCluster,
    string Topic
);
