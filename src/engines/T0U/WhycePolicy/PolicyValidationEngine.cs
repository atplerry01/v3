namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("PolicyValidation", EngineTier.T0U, EngineKind.Validation, "PolicyValidationRequest", typeof(EngineEvent))]
public sealed class PolicyValidationEngine : IEngine
{
    public string Name => "PolicyValidation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var policyType = context.Data.GetValueOrDefault("policyType") as string ?? "default";
        var isValid = ValidatePolicy(policyType, context.Data);

        if (!isValid)
            return Task.FromResult(EngineResult.Fail($"Policy validation failed for type: {policyType}"));

        var events = new[]
        {
            EngineEvent.Create("PolicyValidated", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["policyType"] = policyType })
        };

        return Task.FromResult(EngineResult.Ok(events));
    }

    private static bool ValidatePolicy(string policyType, IReadOnlyDictionary<string, object> data)
    {
        return policyType switch
        {
            "identity" => data.ContainsKey("userId"),
            "economic" => data.ContainsKey("amount") && data.ContainsKey("currency"),
            "governance" => data.ContainsKey("proposalId"),
            _ => true
        };
    }
}
