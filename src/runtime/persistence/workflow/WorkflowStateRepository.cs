namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using Whycespace.Contracts.Workflows;
using Whycespace.Runtime.Persistence.Abstractions;

public sealed class WorkflowStateRepository : IWorkflowStateRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WorkflowStateRepository>? _logger;

    public WorkflowStateRepository(string connectionString, ILogger<WorkflowStateRepository>? logger = null)
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
            CREATE TABLE IF NOT EXISTS workflow_states (
                workflow_id TEXT PRIMARY KEY,
                current_step_id TEXT NOT NULL,
                status INTEGER NOT NULL,
                context JSONB NOT NULL DEFAULT '{}',
                started_at TIMESTAMPTZ NOT NULL,
                completed_at TIMESTAMPTZ,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            CREATE INDEX IF NOT EXISTS idx_workflow_status ON workflow_states(status);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveAsync(WorkflowState state)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO workflow_states (workflow_id, current_step_id, status, context, started_at, completed_at, updated_at)
                VALUES (@wid, @step, @status, @ctx::jsonb, @started, @completed, NOW())
                ON CONFLICT (workflow_id)
                DO UPDATE SET current_step_id = @step, status = @status, context = @ctx::jsonb, completed_at = @completed, updated_at = NOW()
                """;
            cmd.Parameters.AddWithValue("wid", state.WorkflowId);
            cmd.Parameters.AddWithValue("step", state.CurrentStepId);
            cmd.Parameters.AddWithValue("status", (int)state.Status);
            cmd.Parameters.AddWithValue("ctx", JsonSerializer.Serialize(state.Context));
            cmd.Parameters.AddWithValue("started", state.StartedAt);
            cmd.Parameters.AddWithValue("completed", (object?)state.CompletedAt ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save workflow state {WorkflowId}", state.WorkflowId);
        }
    }

    public async Task<WorkflowState?> GetAsync(string workflowId)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT workflow_id, current_step_id, status, context, started_at, completed_at FROM workflow_states WHERE workflow_id = @wid";
            cmd.Parameters.AddWithValue("wid", workflowId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var context = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(3))
                    ?? new Dictionary<string, object>();
                return new WorkflowState(
                    reader.GetString(0),
                    reader.GetString(1),
                    (WorkflowStatus)reader.GetInt32(2),
                    context,
                    reader.GetFieldValue<DateTimeOffset>(4),
                    reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get workflow state {WorkflowId}", workflowId);
        }
        return null;
    }

    public async Task<IReadOnlyList<WorkflowState>> GetByStatusAsync(WorkflowStatus status)
    {
        var states = new List<WorkflowState>();
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT workflow_id, current_step_id, status, context, started_at, completed_at FROM workflow_states WHERE status = @status ORDER BY started_at DESC";
            cmd.Parameters.AddWithValue("status", (int)status);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var context = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(3))
                    ?? new Dictionary<string, object>();
                states.Add(new WorkflowState(
                    reader.GetString(0),
                    reader.GetString(1),
                    (WorkflowStatus)reader.GetInt32(2),
                    context,
                    reader.GetFieldValue<DateTimeOffset>(4),
                    reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get workflow states by status {Status}", status);
        }
        return states;
    }
}
