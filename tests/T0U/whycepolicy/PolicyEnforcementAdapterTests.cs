namespace Whycespace.WhycePolicy.Tests;

using Whycespace.Engines.T4A.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using PolicyEnforcementResult = Whycespace.Engines.T4A.WhycePolicy.PolicyEnforcementResult;

public sealed class PolicyEnforcementAdapterTests
{
    private readonly PolicyEnforcementAdapter _adapter = new();

    [Fact]
    public void Enforce_AllowDecision_ReturnsAllowExecution()
    {
        var input = CreateInput(
            new PolicyDecision("policy-1", true, "allow", "All policies passed", DateTime.UtcNow));

        var result = _adapter.Enforce(input);

        Assert.True(result.Allowed);
        Assert.Equal(EnforcementAction.AllowExecution, result.EnforcementAction);
        Assert.Contains("Execution allowed", result.Reason);
    }

    [Fact]
    public void Enforce_DenyDecision_ReturnsDenyExecution()
    {
        var input = CreateInput(
            new PolicyDecision("policy-1", false, "deny", "Insufficient permissions", DateTime.UtcNow));

        var result = _adapter.Enforce(input);

        Assert.False(result.Allowed);
        Assert.Equal(EnforcementAction.DenyExecution, result.EnforcementAction);
        Assert.Contains("Execution denied", result.Reason);
    }

    [Fact]
    public void Enforce_GuardianApprovalRequired_ReturnsRequireGuardianApproval()
    {
        var input = CreateInput(
            new PolicyDecision("policy-1", false, "require_guardian", "Guardian must approve", DateTime.UtcNow));

        var result = _adapter.Enforce(input);

        Assert.False(result.Allowed);
        Assert.Equal(EnforcementAction.RequireGuardianApproval, result.EnforcementAction);
        Assert.Contains("Guardian approval required", result.Reason);
    }

    [Fact]
    public void Enforce_QuorumApprovalRequired_ReturnsRequireQuorumApproval()
    {
        var input = CreateInput(
            new PolicyDecision("policy-1", false, "require_quorum", "Quorum vote needed", DateTime.UtcNow));

        var result = _adapter.Enforce(input);

        Assert.False(result.Allowed);
        Assert.Equal(EnforcementAction.RequireQuorumApproval, result.EnforcementAction);
        Assert.Contains("Quorum approval required", result.Reason);
    }

    [Fact]
    public void Enforce_GovernanceEscalation_ReturnsEscalateToGovernance()
    {
        var input = CreateInput(
            new PolicyDecision("policy-1", false, "escalate", "Constitutional policy violated", DateTime.UtcNow));

        var result = _adapter.Enforce(input);

        Assert.False(result.Allowed);
        Assert.Equal(EnforcementAction.EscalateToGovernance, result.EnforcementAction);
        Assert.Contains("Governance escalation required", result.Reason);
    }

    [Fact]
    public void Enforce_SameInput_ReturnsDeterministicResult()
    {
        var decision = new PolicyDecision("policy-1", false, "deny", "Access denied", DateTime.UtcNow);
        var input = CreateInput(decision);

        var result1 = _adapter.Enforce(input);
        var result2 = _adapter.Enforce(input);

        Assert.Equal(result1.Allowed, result2.Allowed);
        Assert.Equal(result1.EnforcementAction, result2.EnforcementAction);
        Assert.Equal(result1.Reason, result2.Reason);
    }

    [Fact]
    public void Enforce_ConcurrentCalls_ThreadSafe()
    {
        var decisions = Enumerable.Range(0, 100).Select(i =>
            new PolicyDecision($"policy-{i}", i % 2 == 0, i % 2 == 0 ? "allow" : "deny",
                $"Reason {i}", DateTime.UtcNow)).ToList();

        var inputs = decisions.Select(d => CreateInput(d)).ToList();

        var results = new PolicyEnforcementResult[inputs.Count];
        Parallel.For(0, inputs.Count, i =>
        {
            results[i] = _adapter.Enforce(inputs[i]);
        });

        for (var i = 0; i < inputs.Count; i++)
        {
            var expected = i % 2 == 0;
            Assert.Equal(expected, results[i].Allowed);
            Assert.Equal(
                expected ? EnforcementAction.AllowExecution : EnforcementAction.DenyExecution,
                results[i].EnforcementAction);
        }
    }

    private static PolicyEnforcementInput CreateInput(PolicyDecision finalDecision)
    {
        return new PolicyEnforcementInput(
            FinalDecision: finalDecision,
            Decisions: new List<PolicyDecision> { finalDecision },
            CommandType: "CreateResource",
            ResourceType: "Document",
            ResourceId: "resource-001",
            ActorId: "actor-001"
        );
    }
}
