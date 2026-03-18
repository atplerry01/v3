namespace Whycespace.Systems.Midstream.HEOS.Context;

using Whycespace.Contracts.Policy;

public sealed class HEOSPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public HEOSPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateSignalProcessingAsync(
        string signalType, string clusterId, string initiatorId)
    {
        var context = new PolicyContext(
            initiatorId,
            clusterId,
            "ProcessSignal",
            new Dictionary<string, object>
            {
                ["signalType"] = signalType,
                ["clusterId"] = clusterId
            });

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
