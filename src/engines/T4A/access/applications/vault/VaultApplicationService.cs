namespace Whycespace.Engines.T4A.Access.Applications.Vault;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Mappings;
using Whycespace.Engines.T4A.Access.Contracts.Requests;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed class VaultApplicationService
{
    private readonly IPlatformDispatcher _dispatcher;

    public VaultApplicationService(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<VaultResponse>> CreateAsync(
        CreateVaultRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<VaultResponse>.Ok(
                ResultToResponseMapper.ToVault(result), correlationId)
            : ApiResponse<VaultResponse>.Fail(
                result.Error ?? "Vault creation failed", correlationId);
    }

    public async Task<ApiResponse<VaultResponse>> TransferAsync(
        TransferVaultRequest request, string correlationId)
    {
        var (command, payload) = RequestToCommandMapper.Map(request);
        var result = await _dispatcher.DispatchAsync(command, payload);

        return result.Success
            ? ApiResponse<VaultResponse>.Ok(
                ResultToResponseMapper.ToVault(result), correlationId)
            : ApiResponse<VaultResponse>.Fail(
                result.Error ?? "Vault transfer failed", correlationId);
    }
}
