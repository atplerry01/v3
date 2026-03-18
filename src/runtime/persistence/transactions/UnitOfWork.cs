namespace Whycespace.Runtime.Persistence.Transactions;

using System.Data;
using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UnitOfWork>? _logger;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(IDbConnectionFactory connectionFactory, ILogger<UnitOfWork>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        _logger?.LogDebug("Transaction started");
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _transaction?.Commit();
        _logger?.LogDebug("Transaction committed");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        _transaction?.Rollback();
        _logger?.LogWarning("Transaction rolled back");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is IAsyncDisposable asyncTx)
            await asyncTx.DisposeAsync();
        else
            _transaction?.Dispose();

        if (_connection is IAsyncDisposable asyncConn)
            await asyncConn.DisposeAsync();
        else
            _connection?.Dispose();
    }
}
