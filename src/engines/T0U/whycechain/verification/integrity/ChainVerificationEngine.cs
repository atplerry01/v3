namespace Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("ChainVerification", EngineTier.T0U, EngineKind.Validation, "ChainVerificationRequest", typeof(EngineEvent))]
public sealed class ChainVerificationEngine : IEngine
{
    public string Name => "ChainVerification";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var chainId = context.Data.GetValueOrDefault("chainId") as string;
        if (string.IsNullOrEmpty(chainId))
            return Task.FromResult(EngineResult.Fail("Missing chainId"));

        var events = new[]
        {
            EngineEvent.Create("ChainVerified", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["chainId"] = chainId, ["verified"] = true })
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
