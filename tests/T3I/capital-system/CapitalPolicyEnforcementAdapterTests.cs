namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Domain.Economic.Capital;
using Whycespace.Engines.T2E.Economic.Capital.Adapters;
using Whycespace.Engines.T2E.Economic.Capital.Shared.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class CapitalPolicyEnforcementAdapterTests
{
    private readonly CapitalRegistry _registry = new();
    private readonly CapitalPolicyEnforcementAdapter _adapter;

    public CapitalPolicyEnforcementAdapterTests()
    {
        _adapter = new CapitalPolicyEnforcementAdapter(_registry);
    }

    private static CapitalPolicyContext CreateContext(
        CapitalOperationType operationType = CapitalOperationType.Contribution,
        Guid? poolId = null,
        Guid? investorId = null,
        decimal amount = 5000m,
        string currency = "GBP",
        Guid? clusterId = null,
        Guid? subClusterId = null,
        Guid? spvId = null,
        Guid? requestedBy = null)
    {
        return new CapitalPolicyContext(
            operationType,
            poolId ?? Guid.NewGuid(),
            investorId ?? Guid.NewGuid(),
            Guid.Empty,
            Guid.Empty,
            amount,
            currency,
            clusterId ?? Guid.NewGuid(),
            subClusterId ?? Guid.NewGuid(),
            spvId ?? Guid.NewGuid(),
            requestedBy ?? Guid.NewGuid(),
            DateTime.UtcNow
        );
    }

    private static CapitalRecord CreateCapitalRecord(
        Guid? poolId = null,
        Guid? ownerId = null,
        decimal amount = 1000m)
    {
        return new CapitalRecord(
            CapitalId: Guid.NewGuid(),
            CapitalType: CapitalType.Contribution,
            PoolId: poolId ?? Guid.NewGuid(),
            OwnerIdentityId: ownerId ?? Guid.NewGuid(),
            ClusterId: Guid.NewGuid(),
            SubClusterId: Guid.NewGuid(),
            SPVId: Guid.NewGuid(),
            Amount: amount,
            Currency: "GBP",
            Status: CapitalStatus.Registered,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PolicyApprovalFlow_ReturnsAllowed()
    {
        var context = CreateContext();

        var decision = _adapter.Enforce(context, policyContext =>
            new PolicyDecision(
                "policy-cap-001",
                true,
                "Allow",
                CapitalPolicyDecisionReasons.PolicyApproved,
                DateTime.UtcNow
            ));

        Assert.True(decision.IsAllowed);
        Assert.Equal(CapitalPolicyDecisionReasons.PolicyApproved, decision.DecisionReason);
        Assert.Equal("policy-cap-001", decision.PolicyId);
    }

    [Fact]
    public void PolicyDenialFlow_ReturnsDenied()
    {
        var context = CreateContext();

        var decision = _adapter.Enforce(context, policyContext =>
            new PolicyDecision(
                "policy-cap-002",
                false,
                "Deny",
                CapitalPolicyDecisionReasons.InvestorLimitExceeded,
                DateTime.UtcNow
            ));

        Assert.False(decision.IsAllowed);
        Assert.Equal(CapitalPolicyDecisionReasons.InvestorLimitExceeded, decision.DecisionReason);
    }

    [Fact]
    public void PoolCapValidation_IncludesPoolTotalInAttributes()
    {
        var poolId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _registry.RegisterCapital(CreateCapitalRecord(poolId: poolId, ownerId: ownerId, amount: 3000m));
        _registry.RegisterCapital(CreateCapitalRecord(poolId: poolId, ownerId: Guid.NewGuid(), amount: 2000m));

        var context = CreateContext(poolId: poolId, investorId: ownerId, amount: 5000m);

        IReadOnlyDictionary<string, string>? capturedAttributes = null;

        _adapter.Enforce(context, policyContext =>
        {
            capturedAttributes = policyContext.Attributes;
            return new PolicyDecision(
                "policy-cap-003",
                false,
                "Deny",
                CapitalPolicyDecisionReasons.PoolCapReached,
                DateTime.UtcNow
            );
        });

        Assert.NotNull(capturedAttributes);
        Assert.Equal("2", capturedAttributes["poolCurrentCapitalCount"]);
        Assert.Equal("5000", capturedAttributes["poolCurrentCapitalTotal"]);
    }

    [Fact]
    public void InvestorLimitValidation_IncludesInvestorTotalInAttributes()
    {
        var investorId = Guid.NewGuid();

        _registry.RegisterCapital(CreateCapitalRecord(ownerId: investorId, amount: 10000m));
        _registry.RegisterCapital(CreateCapitalRecord(ownerId: investorId, amount: 5000m));

        var context = CreateContext(investorId: investorId, amount: 2000m);

        IReadOnlyDictionary<string, string>? capturedAttributes = null;

        _adapter.Enforce(context, policyContext =>
        {
            capturedAttributes = policyContext.Attributes;
            return new PolicyDecision(
                "policy-cap-004",
                false,
                "Deny",
                CapitalPolicyDecisionReasons.InvestorLimitExceeded,
                DateTime.UtcNow
            );
        });

        Assert.NotNull(capturedAttributes);
        Assert.Equal("2", capturedAttributes["investorExistingCapitalCount"]);
        Assert.Equal("15000", capturedAttributes["investorExistingCapitalTotal"]);
    }

    [Fact]
    public async Task ConcurrentPolicyEvaluations_AreThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var context = CreateContext();
                    var decision = _adapter.Enforce(context, policyContext =>
                        new PolicyDecision(
                            $"policy-concurrent-{i}",
                            true,
                            "Allow",
                            CapitalPolicyDecisionReasons.PolicyApproved,
                            DateTime.UtcNow
                        ));

                    Assert.True(decision.IsAllowed);
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    [Fact]
    public void BuildContext_SetsAllFields()
    {
        var poolId = Guid.NewGuid();
        var investorId = Guid.NewGuid();
        var clusterId = Guid.NewGuid();
        var subClusterId = Guid.NewGuid();
        var spvId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();

        var context = _adapter.BuildContext(
            CapitalOperationType.Allocation,
            poolId,
            investorId,
            7500m,
            "USD",
            clusterId,
            subClusterId,
            spvId,
            requestedBy);

        Assert.Equal(CapitalOperationType.Allocation, context.OperationType);
        Assert.Equal(poolId, context.PoolId);
        Assert.Equal(investorId, context.InvestorIdentityId);
        Assert.Equal(7500m, context.Amount);
        Assert.Equal("USD", context.Currency);
        Assert.Equal(clusterId, context.ClusterId);
        Assert.Equal(subClusterId, context.SubClusterId);
        Assert.Equal(spvId, context.SPVId);
        Assert.Equal(requestedBy, context.RequestedBy);
    }

    [Fact]
    public void CreateRequest_MapsOperationTypeCorrectly()
    {
        var operationMappings = new Dictionary<CapitalOperationType, string>
        {
            { CapitalOperationType.Commitment, CapitalPolicyOperations.CommitCapital },
            { CapitalOperationType.Contribution, CapitalPolicyOperations.ContributeCapital },
            { CapitalOperationType.Reservation, CapitalPolicyOperations.ReserveCapital },
            { CapitalOperationType.Allocation, CapitalPolicyOperations.AllocateCapital },
            { CapitalOperationType.Utilization, CapitalPolicyOperations.UtilizeCapital },
            { CapitalOperationType.Distribution, CapitalPolicyOperations.DistributeCapital },
        };

        foreach (var (opType, expectedOperation) in operationMappings)
        {
            var context = CreateContext(operationType: opType);
            var request = _adapter.CreateRequest(context);

            Assert.Equal("CapitalGovernance", request.PolicyDomain);
            Assert.Equal(expectedOperation, request.Operation);
        }
    }

    [Fact]
    public void TranslateDecision_MapsFieldsCorrectly()
    {
        var evaluatedAt = DateTime.UtcNow;
        var policyDecision = new PolicyDecision(
            "policy-translate-001",
            true,
            "Allow",
            "POLICY_APPROVED",
            evaluatedAt
        );

        var result = _adapter.TranslateDecision(policyDecision);

        Assert.True(result.IsAllowed);
        Assert.Equal("POLICY_APPROVED", result.DecisionReason);
        Assert.Equal("policy-translate-001", result.PolicyId);
        Assert.Equal(evaluatedAt, result.EvaluatedAt);
    }

    [Fact]
    public void Enforce_PassesCorrectPolicyDomainToEvaluator()
    {
        var context = CreateContext();
        string? capturedDomain = null;

        _adapter.Enforce(context, policyContext =>
        {
            capturedDomain = policyContext.TargetDomain;
            return new PolicyDecision("p1", true, "Allow", "POLICY_APPROVED", DateTime.UtcNow);
        });

        Assert.Equal("CapitalGovernance", capturedDomain);
    }

    [Fact]
    public void Enforce_PassesInvestorIdentityAsActorId()
    {
        var investorId = Guid.NewGuid();
        var context = CreateContext(investorId: investorId);
        Guid? capturedActorId = null;

        _adapter.Enforce(context, policyContext =>
        {
            capturedActorId = policyContext.ActorId;
            return new PolicyDecision("p1", true, "Allow", "POLICY_APPROVED", DateTime.UtcNow);
        });

        Assert.Equal(investorId, capturedActorId);
    }
}
