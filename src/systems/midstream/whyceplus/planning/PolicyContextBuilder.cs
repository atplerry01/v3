namespace Whycespace.Systems.Midstream.WhycePlus.Planning;

using Whycespace.Contracts.Policy;

public static class WhycePlusPolicyContextBuilder
{
    public static PolicyContext BuildDirectiveContext(string directiveId, string scope, string initiatorId)
    {
        return new PolicyContext(
            initiatorId,
            directiveId,
            "IssuePlanningDirective",
            new Dictionary<string, object>
            {
                ["scope"] = scope,
                ["directiveId"] = directiveId
            });
    }

    public static PolicyContext BuildOptimizationContext(string resourceType, string poolId)
    {
        return new PolicyContext(
            poolId,
            resourceType,
            "OptimizeResource",
            new Dictionary<string, object>
            {
                ["resourceType"] = resourceType
            });
    }
}
