namespace Whycespace.Runtime.Persistence.Diagnostics;

using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class PersistenceHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PersistenceHealthCheck>? _logger;

    public PersistenceHealthCheck(IDbConnectionFactory connectionFactory, ILogger<PersistenceHealthCheck>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<bool> CheckPostgresAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken) as IAsyncDisposable;
            _logger?.LogDebug("Postgres health check passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Postgres health check failed");
            return false;
        }
    }

    public async Task<PersistenceHealthReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var postgresHealthy = await CheckPostgresAsync(cancellationToken);

        return new PersistenceHealthReport(
            IsHealthy: postgresHealthy,
            PostgresConnected: postgresHealthy,
            TotalReads: PersistenceMetrics.TotalReads,
            TotalWrites: PersistenceMetrics.TotalWrites,
            TotalErrors: PersistenceMetrics.TotalErrors,
            CheckedAt: DateTimeOffset.UtcNow
        );
    }
}

public sealed record PersistenceHealthReport(
    bool IsHealthy,
    bool PostgresConnected,
    long TotalReads,
    long TotalWrites,
    long TotalErrors,
    DateTimeOffset CheckedAt
);
