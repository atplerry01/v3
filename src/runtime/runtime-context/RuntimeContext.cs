namespace Whycespace.Runtime.Context;

public sealed class RuntimeContext
{
    public ExecutionContext Execution { get; }
    public CorrelationContext Correlation { get; }
    public TenantContext Tenant { get; }
    public RequestContext Request { get; }

    public RuntimeContext(
        ExecutionContext execution,
        CorrelationContext correlation,
        TenantContext tenant,
        RequestContext request)
    {
        Execution = execution;
        Correlation = correlation;
        Tenant = tenant;
        Request = request;
    }

    public static RuntimeContext Create(string tenantId, string? correlationId = null)
    {
        var corrId = correlationId ?? Guid.NewGuid().ToString();
        return new RuntimeContext(
            new ExecutionContext(Guid.NewGuid(), DateTimeOffset.UtcNow),
            new CorrelationContext(corrId, null, corrId),
            new TenantContext(tenantId),
            new RequestContext(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow));
    }
}
