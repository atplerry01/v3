namespace Whycespace.Runtime.Persistence.Abstractions;

using System.Data;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    string ConnectionString { get; }
}
