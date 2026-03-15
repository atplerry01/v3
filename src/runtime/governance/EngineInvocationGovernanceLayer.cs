namespace Whycespace.RuntimeGovernance;

public sealed class EngineInvocationGovernanceLayer
{
    private readonly EngineCapabilityRegistry _registry;
    private readonly IEnginePolicyEvaluator _policyEvaluator;
    private readonly InvocationLimiter _limiter;

    public EngineInvocationGovernanceLayer(
        EngineCapabilityRegistry registry,
        IEnginePolicyEvaluator policyEvaluator,
        InvocationLimiter? limiter = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _policyEvaluator = policyEvaluator ?? throw new ArgumentNullException(nameof(policyEvaluator));
        _limiter = limiter ?? new InvocationLimiter();
    }

    public EngineInvocationGovernanceResult Evaluate(EngineInvocationGovernanceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Engine exists in registry
        if (!_registry.EngineExists(command.EngineName, command.EngineVersion))
        {
            return Denied(command, Guid.Empty,
                $"Engine '{command.EngineName}' version '{command.EngineVersion}' is not registered.");
        }

        var entry = _registry.GetEngine(command.EngineName, command.EngineVersion)!;

        // 2. Engine version is supported (registry lookup already validates this)

        // 3. Engine tier compatibility — T0U engines cannot be invoked by workflow steps directly
        if (entry.ExecutionTier == Runtime.EngineManifest.Models.EngineTier.T0U)
        {
            return Rejected(command, Guid.Empty,
                $"Engine '{command.EngineName}' is a constitutional (T0U) engine and cannot be invoked by workflow steps.");
        }

        // 4. Invocation limits check
        if (!_limiter.TryAcquire(command.EngineName, command.WorkflowInstanceId))
        {
            return Rejected(command, Guid.Empty,
                $"Invocation limit exceeded for engine '{command.EngineName}' in workflow '{command.WorkflowInstanceId}'.");
        }

        // 5. WHYCEPOLICY evaluation
        var policyContext = new EnginePolicyContext(
            command.WorkflowInstanceId,
            command.EngineName,
            command.EngineVersion,
            command.RequestedBy,
            command.WorkflowStepId,
            command.CorrelationId);

        var policyResult = _policyEvaluator.Evaluate(policyContext);

        if (policyResult.Decision == PolicyDecision.Deny)
        {
            return Denied(command, policyResult.PolicyEvaluationId,
                $"Policy denied invocation: {policyResult.Reason}");
        }

        // 6. Approved (Allow or ConditionalAllow both pass governance)
        return new EngineInvocationGovernanceResult(
            command.InvocationId,
            command.EngineName,
            GovernanceDecision.Approved,
            policyResult.Decision == PolicyDecision.ConditionalAllow
                ? $"Conditionally approved: {policyResult.Reason}"
                : "Invocation approved.",
            policyResult.PolicyEvaluationId,
            DateTime.UtcNow);
    }

    private static EngineInvocationGovernanceResult Denied(
        EngineInvocationGovernanceCommand command, Guid policyEvaluationId, string reason)
    {
        return new EngineInvocationGovernanceResult(
            command.InvocationId,
            command.EngineName,
            GovernanceDecision.Denied,
            reason,
            policyEvaluationId,
            DateTime.UtcNow);
    }

    private static EngineInvocationGovernanceResult Rejected(
        EngineInvocationGovernanceCommand command, Guid policyEvaluationId, string reason)
    {
        return new EngineInvocationGovernanceResult(
            command.InvocationId,
            command.EngineName,
            GovernanceDecision.Rejected,
            reason,
            policyEvaluationId,
            DateTime.UtcNow);
    }
}

public sealed class InvocationLimiter
{
    private readonly int _maxInvocationsPerEngine;
    private readonly Dictionary<string, int> _counts = new();

    public InvocationLimiter(int maxInvocationsPerEngine = 100)
    {
        _maxInvocationsPerEngine = maxInvocationsPerEngine;
    }

    public bool TryAcquire(string engineName, Guid workflowInstanceId)
    {
        var key = $"{engineName}:{workflowInstanceId}";

        if (!_counts.TryGetValue(key, out var count))
            count = 0;

        if (count >= _maxInvocationsPerEngine)
            return false;

        _counts[key] = count + 1;
        return true;
    }

    public void Reset(string engineName, Guid workflowInstanceId)
    {
        var key = $"{engineName}:{workflowInstanceId}";
        _counts.Remove(key);
    }
}
