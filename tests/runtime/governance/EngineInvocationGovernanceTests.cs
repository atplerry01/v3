namespace Whycespace.RuntimeGovernance.Tests;

using Whycespace.Runtime.EngineManifest.Models;

public sealed class EngineInvocationGovernanceTests
{
    private readonly EngineCapabilityRegistry _registry;
    private readonly EngineInvocationGovernanceLayer _layer;
    private readonly StubPolicyEvaluator _policyEvaluator;

    public EngineInvocationGovernanceTests()
    {
        _registry = new EngineCapabilityRegistry();
        _policyEvaluator = new StubPolicyEvaluator();
        _layer = new EngineInvocationGovernanceLayer(_registry, _policyEvaluator);

        _registry.Register(new EngineRegistryEntry(
            "VaultContributionEngine", "1.0.0",
            new[] { "RecordContribution" },
            "Economic", EngineTier.T2E));
    }

    [Fact]
    public void Evaluate_ValidEngineInvocation_ReturnsApproved()
    {
        var command = CreateCommand("VaultContributionEngine", "1.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Approved, result.GovernanceDecision);
        Assert.Equal(command.InvocationId, result.InvocationId);
        Assert.Equal("VaultContributionEngine", result.EngineName);
        Assert.Equal("Invocation approved.", result.DecisionReason);
    }

    [Fact]
    public void Evaluate_EngineNotFound_ReturnsDenied()
    {
        var command = CreateCommand("NonExistentEngine", "1.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Denied, result.GovernanceDecision);
        Assert.Contains("not registered", result.DecisionReason);
    }

    [Fact]
    public void Evaluate_UnsupportedEngineVersion_ReturnsDenied()
    {
        var command = CreateCommand("VaultContributionEngine", "99.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Denied, result.GovernanceDecision);
        Assert.Contains("not registered", result.DecisionReason);
    }

    [Fact]
    public void Evaluate_PolicyDenial_ReturnsDenied()
    {
        _policyEvaluator.SetDecision(PolicyDecision.Deny, "Insufficient permissions");
        var command = CreateCommand("VaultContributionEngine", "1.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Denied, result.GovernanceDecision);
        Assert.Contains("Policy denied", result.DecisionReason);
        Assert.Contains("Insufficient permissions", result.DecisionReason);
    }

    [Fact]
    public void Evaluate_ConstitutionalEngine_ReturnsRejected()
    {
        _registry.Register(new EngineRegistryEntry(
            "WhycePolicyEngine", "1.0.0",
            new[] { "EvaluatePolicy" },
            "Constitutional", EngineTier.T0U));

        var command = CreateCommand("WhycePolicyEngine", "1.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Rejected, result.GovernanceDecision);
        Assert.Contains("constitutional", result.DecisionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_InvocationLimitExceeded_ReturnsRejected()
    {
        var limiter = new InvocationLimiter(maxInvocationsPerEngine: 2);
        var layer = new EngineInvocationGovernanceLayer(_registry, _policyEvaluator, limiter);

        var workflowId = Guid.NewGuid();
        var cmd1 = CreateCommand("VaultContributionEngine", "1.0.0", workflowId);
        var cmd2 = CreateCommand("VaultContributionEngine", "1.0.0", workflowId);
        var cmd3 = CreateCommand("VaultContributionEngine", "1.0.0", workflowId);

        layer.Evaluate(cmd1);
        layer.Evaluate(cmd2);
        var result = layer.Evaluate(cmd3);

        Assert.Equal(GovernanceDecision.Rejected, result.GovernanceDecision);
        Assert.Contains("limit exceeded", result.DecisionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_ConditionalAllow_ReturnsApprovedWithReason()
    {
        _policyEvaluator.SetDecision(PolicyDecision.ConditionalAllow, "Requires audit logging");
        var command = CreateCommand("VaultContributionEngine", "1.0.0");

        var result = _layer.Evaluate(command);

        Assert.Equal(GovernanceDecision.Approved, result.GovernanceDecision);
        Assert.Contains("Conditionally approved", result.DecisionReason);
        Assert.Contains("Requires audit logging", result.DecisionReason);
    }

    [Fact]
    public void Evaluate_IsDeterministic_SameInputProducesSameDecision()
    {
        var command = CreateCommand("VaultContributionEngine", "1.0.0");

        var result1 = _layer.Evaluate(command);
        var result2 = _layer.Evaluate(command);

        Assert.Equal(result1.GovernanceDecision, result2.GovernanceDecision);
        Assert.Equal(result1.DecisionReason, result2.DecisionReason);
        Assert.Equal(result1.EngineName, result2.EngineName);
    }

    private static EngineInvocationGovernanceCommand CreateCommand(
        string engineName, string engineVersion, Guid? workflowInstanceId = null)
    {
        return new EngineInvocationGovernanceCommand(
            InvocationId: Guid.NewGuid(),
            WorkflowInstanceId: workflowInstanceId ?? Guid.NewGuid(),
            WorkflowStepId: "step-1",
            EngineName: engineName,
            EngineVersion: engineVersion,
            RequestedBy: "test-user",
            CorrelationId: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow);
    }

    private sealed class StubPolicyEvaluator : IEnginePolicyEvaluator
    {
        private PolicyDecision _decision = PolicyDecision.Allow;
        private string _reason = "Allowed by default";

        public void SetDecision(PolicyDecision decision, string reason)
        {
            _decision = decision;
            _reason = reason;
        }

        public PolicyEvaluationResult Evaluate(EnginePolicyContext context)
        {
            return new PolicyEvaluationResult(Guid.NewGuid(), _decision, _reason);
        }
    }
}
