namespace Whycespace.Engines.T4A.Auth;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("Authentication", EngineTier.T4A, EngineKind.Validation, "AuthenticationRequest", typeof(EngineEvent))]
public sealed class AuthenticationEngine : IEngine
{
    public string Name => "Authentication";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var token = context.Data.GetValueOrDefault("token") as string;
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(EngineResult.Fail("Missing authentication token"));

        var userId = Guid.NewGuid().ToString();

        var events = new[]
        {
            EngineEvent.Create("UserAuthenticated", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["userId"] = userId })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["userId"] = userId, ["authenticated"] = true }));
    }
}
