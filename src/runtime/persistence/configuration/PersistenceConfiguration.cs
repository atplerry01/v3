namespace Whycespace.Runtime.Persistence.Configuration;

using Whycespace.Runtime.Persistence.Connection;

public sealed record PersistenceConfiguration
{
    public ConnectionOptions Connection { get; init; } = new();
    public bool AutoInitialize { get; init; } = true;
    public bool EnableDiagnostics { get; init; } = true;
    public bool EnableMigrations { get; init; } = false;
    public string SchemaName { get; init; } = "public";
}
