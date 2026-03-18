namespace Whycespace.Runtime.Persistence.Projections;

using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Persistence.Abstractions;
using Whycespace.Runtime.Persistence.Connection;

public sealed class RedisProjectionStore : IProjectionStore
{
    private readonly RedisConnectionFactory _connectionFactory;
    private readonly ILogger<RedisProjectionStore>? _logger;

    public RedisProjectionStore(RedisConnectionFactory connectionFactory, ILogger<RedisProjectionStore>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        _logger?.LogInformation("RedisProjectionStore initialized");
        return Task.CompletedTask;
    }

    public Task UpsertAsync(string projectionName, string key, object state)
    {
        _logger?.LogDebug("Redis upsert: {Projection}/{Key}", projectionName, key);
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string projectionName, string key)
    {
        _logger?.LogDebug("Redis get: {Projection}/{Key}", projectionName, key);
        return Task.FromResult<string?>(null);
    }

    public Task<IReadOnlyList<(string Key, string State)>> GetAllAsync(string projectionName)
    {
        return Task.FromResult<IReadOnlyList<(string Key, string State)>>(Array.Empty<(string, string)>());
    }
}
