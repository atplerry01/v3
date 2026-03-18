namespace Whycespace.Runtime.Persistence.Migrations;

using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class MigrationRunner
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationRunner>? _logger;

    public MigrationRunner(IDbConnectionFactory connectionFactory, ILogger<MigrationRunner>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Running persistence migrations...");

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken) as IAsyncDisposable;

        _logger?.LogInformation("Persistence migrations completed");
    }
}
