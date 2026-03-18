namespace Whycespace.Systems.Midstream.HEOS.Context;

using Whycespace.Contracts.Policy;

public static class HEOSPolicyContextBuilder
{
    public static PolicyContext BuildSignalContext(string signalType, string clusterId, string initiatorId)
    {
        return new PolicyContext(
            initiatorId,
            clusterId,
            "ProcessSignal",
            new Dictionary<string, object>
            {
                ["signalType"] = signalType,
                ["clusterId"] = clusterId,
                ["initiatorId"] = initiatorId
            });
    }

    public static PolicyContext BuildRoutingContext(string sourceCluster, string targetCluster, string routingDecision)
    {
        return new PolicyContext(
            sourceCluster,
            targetCluster,
            "RouteSignal",
            new Dictionary<string, object>
            {
                ["routingDecision"] = routingDecision
            });
    }
}
