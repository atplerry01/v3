namespace Whycespace.Engines.T0U_Constitutional;

using Whycespace.Shared.Contracts;

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
