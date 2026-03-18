namespace Whycespace.Engines.T4A.Access.Applications.Capital;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Mappings;
using Whycespace.Engines.T4A.Access.Contracts.Requests;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed class CapitalApplicationService
{
    private readonly IPlatformDispatcher _dispatcher;

    public CapitalApplicationService(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<CapitalAllocationResponse>> AllocateAsync(
        AllocateCapitalRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<CapitalAllocationResponse>.Ok(
                ResultToResponseMapper.ToCapitalAllocation(result), correlationId)
            : ApiResponse<CapitalAllocationResponse>.Fail(
                result.Error ?? "Capital allocation failed", correlationId);
    }

    public async Task<ApiResponse<CapitalAllocationResponse>> ContributeAsync(
        ContributeCapitalRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<CapitalAllocationResponse>.Ok(
                ResultToResponseMapper.ToCapitalAllocation(result), correlationId)
            : ApiResponse<CapitalAllocationResponse>.Fail(
                result.Error ?? "Capital contribution failed", correlationId);
    }
}
