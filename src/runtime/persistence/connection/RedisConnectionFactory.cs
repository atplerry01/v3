namespace Whycespace.Runtime.Persistence.Connection;

public sealed class RedisConnectionFactory
{
    public string ConnectionString { get; }

    public RedisConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ConnectionString = connectionString;
    }
}
