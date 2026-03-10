namespace Whycespace.Engines.T0U_Constitutional;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("IdentityVerification", EngineTier.T0U, EngineKind.Validation, "IdentityVerificationRequest", typeof(EngineEvent))]
public sealed class IdentityVerificationEngine : IEngine
{
    public string Name => "IdentityVerification";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var userId = context.Data.GetValueOrDefault("userId") as string;
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(EngineResult.Fail("Missing userId"));

        var events = new[]
        {
            EngineEvent.Create("IdentityVerified", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["userId"] = userId, ["verified"] = true })
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
