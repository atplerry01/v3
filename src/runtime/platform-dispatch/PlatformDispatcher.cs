namespace Whycespace.Runtime.PlatformDispatch;

using Whycespace.Contracts.Runtime;
using Whycespace.Runtime.PlatformDispatch.Handlers;

public sealed class PlatformDispatcher : IPlatformDispatcher
{
    private readonly IdentityCommandHandler _identityHandler;
    private readonly PolicyCommandHandler _policyHandler;
    private readonly GovernanceCommandHandler _governanceHandler;
    private readonly WssCommandHandler _wssHandler;

    public PlatformDispatcher(
        IdentityCommandHandler identityHandler,
        PolicyCommandHandler policyHandler,
        GovernanceCommandHandler governanceHandler,
        WssCommandHandler wssHandler)
    {
        _identityHandler = identityHandler;
        _policyHandler = policyHandler;
        _governanceHandler = governanceHandler;
        _wssHandler = wssHandler;
    }

    public Task<DispatchResult> DispatchAsync(string command, Dictionary<string, object> payload)
    {
        if (_identityHandler.CanHandle(command))
            return _identityHandler.HandleAsync(command, payload);

        if (_policyHandler.CanHandle(command))
            return _policyHandler.HandleAsync(command, payload);

        if (_governanceHandler.CanHandle(command))
            return _governanceHandler.HandleAsync(command, payload);

        if (_wssHandler.CanHandle(command))
            return _wssHandler.HandleAsync(command, payload);

        return Task.FromResult(DispatchResult.Fail($"No handler found for command: {command}"));
    }
}
