namespace Whycespace.Runtime.PolicyEnforcement;

using Whycespace.Contracts.Commands;

public sealed class PolicyEvaluationPipeline
{
    private readonly IPolicyDecisionAdapter _adapter;

    public PolicyEvaluationPipeline(IPolicyDecisionAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<PolicyDecisionResult> EvaluateAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var context = new PolicyEvaluationContext(
            CommandType: command.GetType().Name,
            Timestamp: DateTimeOffset.UtcNow);

        return await _adapter.EvaluateAsync(context, cancellationToken);
    }
}

public sealed record PolicyEvaluationContext(
    string CommandType,
    DateTimeOffset Timestamp,
    string? TenantId = null,
    string? OperatorId = null
);
