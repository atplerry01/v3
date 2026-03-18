namespace Whycespace.Platform.Simulation.Policy;

using Whycespace.Contracts.Policy;

/// <summary>
/// Adapts the WhycePolicy evaluation pipeline for simulation use.
/// Evaluates whether a simulated workflow or command is permitted by governance policies
/// without mutating any policy state.
/// </summary>
public sealed class SimulationPolicyAdapter
{
    private readonly IPolicyEvaluator? _policyEvaluator;
    private readonly HashSet<string> _blockedWorkflowTypes = new();
    private readonly HashSet<string> _allowedWorkflowTypes = new();

    public SimulationPolicyAdapter(IPolicyEvaluator? policyEvaluator = null)
    {
        _policyEvaluator = policyEvaluator;
    }

    /// <summary>
    /// Evaluates whether a workflow type is permitted for simulation.
    /// If a real policy evaluator is wired, delegates to WhycePolicy.
    /// Otherwise falls back to local allow/block lists.
    /// </summary>
    public async Task<SimulationPolicyResult> EvaluateAsync(string workflowType, string workflowId)
    {
        // Check local block list first
        if (_blockedWorkflowTypes.Contains(workflowType))
        {
            return SimulationPolicyResult.Deny(
                $"Workflow type '{workflowType}' is blocked for simulation");
        }

        // If we have an allow list and the type is not in it, deny
        if (_allowedWorkflowTypes.Count > 0 && !_allowedWorkflowTypes.Contains(workflowType))
        {
            return SimulationPolicyResult.Deny(
                $"Workflow type '{workflowType}' is not in the simulation allow list");
        }

        // Delegate to WhycePolicy if available
        if (_policyEvaluator is not null)
        {
            var context = new PolicyContext(
                SubjectId: "simulation-system",
                ResourceId: workflowId,
                Action: $"simulate:{workflowType}",
                Attributes: new Dictionary<string, object>
                {
                    ["simulationMode"] = true,
                    ["workflowType"] = workflowType
                });

            var result = await _policyEvaluator.EvaluateAsync(context);
            return new SimulationPolicyResult(
                IsPermitted: result.IsPermitted,
                Violations: result.Violations.ToList());
        }

        return SimulationPolicyResult.Permit();
    }

    public void BlockWorkflowType(string workflowType) => _blockedWorkflowTypes.Add(workflowType);
    public void AllowWorkflowType(string workflowType) => _allowedWorkflowTypes.Add(workflowType);
    public void UnblockWorkflowType(string workflowType) => _blockedWorkflowTypes.Remove(workflowType);
}

public sealed record SimulationPolicyResult(
    bool IsPermitted,
    IReadOnlyList<string> Violations)
{
    public static SimulationPolicyResult Permit() => new(true, Array.Empty<string>());
    public static SimulationPolicyResult Deny(string violation) => new(false, new[] { violation });
}
