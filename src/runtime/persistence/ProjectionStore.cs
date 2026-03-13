namespace Whycespace.Runtime.Persistence;

using global::System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;

public sealed class ProjectionStore
{
    private readonly string _connectionString;
    private readonly ILogger<ProjectionStore>? _logger;

    public ProjectionStore(string connectionString, ILogger<ProjectionStore>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS projections (
                projection_name TEXT NOT NULL,
                projection_key TEXT NOT NULL,
                state JSONB NOT NULL DEFAULT '{}',
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (projection_name, projection_key)
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertAsync(string projectionName, string key, object state)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO projections (projection_name, projection_key, state, updated_at)
                VALUES (@name, @key, @state::jsonb, NOW())
                ON CONFLICT (projection_name, projection_key)
                DO UPDATE SET state = @state::jsonb, updated_at = NOW()
                """;
            cmd.Parameters.AddWithValue("name", projectionName);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("state", JsonSerializer.Serialize(state));
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upsert projection {Name}/{Key}", projectionName, key);
        }
    }

    public async Task<string?> GetAsync(string projectionName, string key)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT state FROM projections WHERE projection_name = @name AND projection_key = @key";
            cmd.Parameters.AddWithValue("name", projectionName);
            cmd.Parameters.AddWithValue("key", key);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get projection {Name}/{Key}", projectionName, key);
            return null;
        }
    }

    public async Task<IReadOnlyList<(string Key, string State)>> GetAllAsync(string projectionName)
    {
        var results = new List<(string, string)>();
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT projection_key, state FROM projections WHERE projection_name = @name ORDER BY updated_at DESC";
            cmd.Parameters.AddWithValue("name", projectionName);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add((reader.GetString(0), reader.GetString(1)));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get all projections for {Name}", projectionName);
        }
        return results;
    }
}
