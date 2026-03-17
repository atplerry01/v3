namespace Whycespace.Engines.T3I.Atlas.Identity.Models;

public sealed record DeviceTrustEvaluationResult(
    Guid IdentityId,
    string DeviceId,
    double TrustScore,
    DeviceTrustLevel TrustLevel,
    List<string> RiskIndicators,
    DateTime EvaluatedAt);

public enum DeviceTrustLevel
{
    Low,
    Medium,
    High
}
