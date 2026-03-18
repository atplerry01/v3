using Whycespace.Engines.T2E.Economic.Capital.Shared.Models;

namespace Whycespace.Engines.T2E.Economic.Capital.Adapters;

using Whycespace.Domain.Core.Economic;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class CapitalPolicyEnforcementAdapter
{
    private readonly ICapitalRegistry _capitalRegistry;

    private const string PolicyDomain = "CapitalGovernance";

    public CapitalPolicyEnforcementAdapter(ICapitalRegistry capitalRegistry)
    {
        _capitalRegistry = capitalRegistry;
    }

    public CapitalPolicyContext BuildContext(
        CapitalOperationType operationType,
        Guid poolId,
        Guid investorIdentityId,
        decimal amount,
        string currency,
        Guid clusterId,
        Guid subClusterId,
        Guid spvId,
        Guid requestedBy,
        Guid reservationId = default,
        Guid allocationId = default)
    {
        return new CapitalPolicyContext(
            operationType,
            poolId,
            investorIdentityId,
            reservationId,
            allocationId,
            amount,
            currency,
            clusterId,
            subClusterId,
            spvId,
            requestedBy,
            DateTime.UtcNow
        );
    }

    public CapitalPolicyRequest CreateRequest(
        CapitalPolicyContext context,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var operation = context.OperationType switch
        {
            CapitalOperationType.Commitment => CapitalPolicyOperations.CommitCapital,
            CapitalOperationType.Contribution => CapitalPolicyOperations.ContributeCapital,
            CapitalOperationType.Reservation => CapitalPolicyOperations.ReserveCapital,
            CapitalOperationType.Allocation => CapitalPolicyOperations.AllocateCapital,
            CapitalOperationType.Utilization => CapitalPolicyOperations.UtilizeCapital,
            CapitalOperationType.Distribution => CapitalPolicyOperations.DistributeCapital,
            _ => throw new ArgumentOutOfRangeException(nameof(context), "Unknown capital operation type.")
        };

        return new CapitalPolicyRequest(
            PolicyDomain,
            operation,
            context,
            metadata ?? new Dictionary<string, string>()
        );
    }

    public CapitalPolicyDecision TranslateDecision(PolicyDecision policyDecision)
    {
        return new CapitalPolicyDecision(
            policyDecision.Allowed,
            policyDecision.Reason,
            policyDecision.PolicyId,
            policyDecision.EvaluatedAt
        );
    }

    public CapitalPolicyDecision Enforce(
        CapitalPolicyContext context,
        Func<PolicyContext, PolicyDecision> policyEvaluator)
    {
        var request = CreateRequest(context);

        var policyContext = new PolicyContext(
            Guid.NewGuid(),
            context.InvestorIdentityId,
            request.PolicyDomain,
            BuildPolicyAttributes(context),
            context.RequestedAt
        );

        var policyDecision = policyEvaluator(policyContext);
        return TranslateDecision(policyDecision);
    }

    private IReadOnlyDictionary<string, string> BuildPolicyAttributes(
        CapitalPolicyContext context)
    {
        var attributes = new Dictionary<string, string>
        {
            ["operationType"] = context.OperationType.ToString(),
            ["poolId"] = context.PoolId.ToString(),
            ["investorIdentityId"] = context.InvestorIdentityId.ToString(),
            ["amount"] = context.Amount.ToString(),
            ["currency"] = context.Currency,
            ["clusterId"] = context.ClusterId.ToString(),
            ["subClusterId"] = context.SubClusterId.ToString(),
            ["spvId"] = context.SPVId.ToString(),
            ["requestedBy"] = context.RequestedBy.ToString()
        };

        if (context.ReservationId != Guid.Empty)
            attributes["reservationId"] = context.ReservationId.ToString();

        if (context.AllocationId != Guid.Empty)
            attributes["allocationId"] = context.AllocationId.ToString();

        var existingCapital = _capitalRegistry.ListCapitalByOwner(context.InvestorIdentityId);
        var poolCapital = _capitalRegistry.ListCapitalByPool(context.PoolId);

        attributes["investorExistingCapitalCount"] = existingCapital.Count.ToString();
        attributes["investorExistingCapitalTotal"] = existingCapital.Sum(c => c.Amount).ToString();
        attributes["poolCurrentCapitalCount"] = poolCapital.Count.ToString();
        attributes["poolCurrentCapitalTotal"] = poolCapital.Sum(c => c.Amount).ToString();

        return attributes;
    }
}
