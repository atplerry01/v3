namespace Whycespace.IntelligenceRuntime.Models;

public sealed record IntelligenceRequest(
    Guid RequestId,
    IntelligenceCapability Capability,
    string EngineId,
    IReadOnlyDictionary<string, object> Parameters,
    string? CorrelationId = null,
    string? PartitionKey = null
)
{
    public static IntelligenceRequest Create(
        IntelligenceCapability capability,
        string engineId,
        IReadOnlyDictionary<string, object> parameters,
        string? correlationId = null)
        => new(Guid.NewGuid(), capability, engineId, parameters, correlationId);
}
