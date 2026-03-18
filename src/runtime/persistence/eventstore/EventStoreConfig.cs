namespace Whycespace.Runtime.Persistence.EventStore;

public sealed record EventStoreConfig(
    string ConnectionString,
    string SchemaName = "public",
    string TableName = "events",
    int BatchSize = 500,
    bool EnableSnapshotting = false,
    int SnapshotInterval = 100
);
