namespace Whycespace.Runtime.Persistence;

using global::System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using Whycespace.Contracts.Events;

public sealed class PostgresEventStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresEventStore>? _logger;

    public PostgresEventStore(string connectionString, ILogger<PostgresEventStore>? logger = null)
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
            CREATE TABLE IF NOT EXISTS events (
                event_id UUID PRIMARY KEY,
                event_type TEXT NOT NULL,
                aggregate_id UUID NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL,
                payload JSONB NOT NULL DEFAULT '{}',
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            CREATE INDEX IF NOT EXISTS idx_events_aggregate ON events(aggregate_id);
            CREATE INDEX IF NOT EXISTS idx_events_type ON events(event_type);
            CREATE INDEX IF NOT EXISTS idx_events_timestamp ON events(timestamp);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AppendAsync(SystemEvent @event)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO events (event_id, event_type, aggregate_id, timestamp, payload)
                VALUES (@id, @type, @agg, @ts, @payload::jsonb)
                ON CONFLICT (event_id) DO NOTHING
                """;
            cmd.Parameters.AddWithValue("id", @event.EventId);
            cmd.Parameters.AddWithValue("type", @event.EventType);
            cmd.Parameters.AddWithValue("agg", @event.AggregateId);
            cmd.Parameters.AddWithValue("ts", @event.Timestamp);
            cmd.Parameters.AddWithValue("payload", JsonSerializer.Serialize(@event.Payload));
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to append event {EventId} of type {EventType}", @event.EventId, @event.EventType);
        }
    }

    public async Task<IReadOnlyList<SystemEvent>> GetByAggregateAsync(Guid aggregateId)
    {
        var events = new List<SystemEvent>();
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT event_id, event_type, aggregate_id, timestamp, payload FROM events WHERE aggregate_id = @agg ORDER BY timestamp";
            cmd.Parameters.AddWithValue("agg", aggregateId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(4))
                    ?? new Dictionary<string, object>();
                events.Add(new SystemEvent(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetGuid(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    payload));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read events for aggregate {AggregateId}", aggregateId);
        }
        return events;
    }
}
