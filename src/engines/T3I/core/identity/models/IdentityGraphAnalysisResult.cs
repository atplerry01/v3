namespace Whycespace.Engines.T3I.Core.Identity.Models;

public sealed record IdentityGraphAnalysisResult(
    Guid IdentityId,
    int ConnectedIdentityCount,
    int SharedDeviceCount,
    int SuspiciousConnections,
    double RiskScore,
    DateTime AnalyzedAt);
