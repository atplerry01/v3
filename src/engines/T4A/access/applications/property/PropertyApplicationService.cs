namespace Whycespace.Engines.T4A.Access.Applications.Property;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Mappings;
using Whycespace.Engines.T4A.Access.Contracts.Requests;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed class PropertyApplicationService
{
    private readonly IPlatformDispatcher _dispatcher;

    public PropertyApplicationService(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<PropertyResponse>> ListAsync(
        ListPropertyRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<PropertyResponse>.Ok(
                ResultToResponseMapper.ToProperty(result), correlationId)
            : ApiResponse<PropertyResponse>.Fail(
                result.Error ?? "Property listing failed", correlationId);
    }
}
