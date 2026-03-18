namespace Whycespace.Runtime.PolicyEnforcement;

using Whycespace.Contracts.Commands;

public sealed class RuntimePolicyMiddleware
{
    private readonly PolicyEvaluationPipeline _pipeline;

    public RuntimePolicyMiddleware(PolicyEvaluationPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<PolicyDecisionResult> EvaluateAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        return await _pipeline.EvaluateAsync(command, cancellationToken);
    }

    public async Task<TResult> InterceptAsync<TResult>(
        ICommand command,
        Func<Task<TResult>> execution,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(command, cancellationToken);

        if (!decision.IsAllowed)
            throw new PolicyViolationException(
                $"Command '{command.GetType().Name}' blocked by policy: {decision.Reason}");

        return await execution();
    }
}
