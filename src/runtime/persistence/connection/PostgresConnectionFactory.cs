namespace Whycespace.Runtime.Persistence.Connection;

using System.Data;
using Npgsql;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class PostgresConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public PostgresConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ConnectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
