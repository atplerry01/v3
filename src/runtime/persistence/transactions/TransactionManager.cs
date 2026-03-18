namespace Whycespace.Runtime.Persistence.Transactions;

using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class TransactionManager
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TransactionManager>? _logger;

    public TransactionManager(IDbConnectionFactory connectionFactory, ILogger<TransactionManager>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var uow = new UnitOfWork(_connectionFactory, _logger != null ? null : null);
        await uow.BeginAsync(cancellationToken);
        try
        {
            var result = await operation(uow);
            await uow.CommitAsync(cancellationToken);
            _logger?.LogDebug("Transaction completed successfully");
            return result;
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<IUnitOfWork, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync<object?>(async uow =>
        {
            await operation(uow);
            return null;
        }, cancellationToken);
    }
}
