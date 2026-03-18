namespace Whycespace.Engines.T4A.Access.Applications.Workforce;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed class WorkforceApplicationService
{
    private readonly IPlatformDispatcher _dispatcher;

    public WorkforceApplicationService(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<IReadOnlyDictionary<string, object>>> AssignAsync(
        string workerId, string taskId, string correlationId)
    {
        var payload = new Dictionary<string, object>
        {
            ["workerId"] = workerId,
            ["taskId"] = taskId
        };

        var result = await _dispatcher.DispatchAsync("workforce.assign", payload);

        return result.Success
            ? ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId)
            : ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Workforce assignment failed", correlationId);
    }

    public async Task<ApiResponse<IReadOnlyDictionary<string, object>>> CheckComplianceAsync(
        string workerId, string correlationId)
    {
        var payload = new Dictionary<string, object>
        {
            ["workerId"] = workerId
        };

        var result = await _dispatcher.DispatchAsync("workforce.compliance.check", payload);

        return result.Success
            ? ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId)
            : ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Compliance check failed", correlationId);
    }
}
