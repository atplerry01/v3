namespace Whycespace.Engines.T4A.Access.Applications.Identity;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Mappings;
using Whycespace.Engines.T4A.Access.Contracts.Requests;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed class IdentityApplicationService
{
    private readonly IPlatformDispatcher _dispatcher;

    public IdentityApplicationService(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<IdentityResponse>> RegisterAsync(
        RegisterIdentityRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<IdentityResponse>.Ok(
                ResultToResponseMapper.ToIdentity(result), correlationId)
            : ApiResponse<IdentityResponse>.Fail(
                result.Error ?? "Identity registration failed", correlationId);
    }

    public async Task<ApiResponse<PolicyEvaluationResponse>> EvaluatePolicyAsync(
        EvaluatePolicyRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<PolicyEvaluationResponse>.Ok(
                ResultToResponseMapper.ToPolicyEvaluation(result), correlationId)
            : ApiResponse<PolicyEvaluationResponse>.Fail(
                result.Error ?? "Policy evaluation failed", correlationId);
    }
}
