namespace Whycespace.Engines.T0U.WhyceID.Recovery;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

public sealed class IdentityRecoveryEvaluationEngine
{
    private readonly IdentityRegistry _registry;

    public IdentityRecoveryEvaluationEngine(IdentityRegistry registry)
    {
        _registry = registry;
    }

    public IdentityRecoveryResult Evaluate(IdentityRecoveryRequest request)
    {
        var evaluatedAt = DateTime.UtcNow;
        var recoveryId = ComputeRecoveryId(request.IdentityId, request.RequestedAt, request.RecoveryMethod);

        // 1. Validate identity existence
        if (!_registry.Exists(request.IdentityId))
        {
            return new IdentityRecoveryResult(
                recoveryId, request.IdentityId, false, request.RecoveryMethod,
                1.0, false, "Identity does not exist", evaluatedAt);
        }

        // 2. Validate identity status
        var identity = _registry.Get(request.IdentityId);
        if (identity.Status == IdentityStatus.Revoked)
        {
            return new IdentityRecoveryResult(
                recoveryId, request.IdentityId, false, request.RecoveryMethod,
                1.0, false, "Identity has been revoked", evaluatedAt);
        }

        // 3. Evaluate verification evidence
        if (string.IsNullOrWhiteSpace(request.VerificationEvidence))
        {
            return new IdentityRecoveryResult(
                recoveryId, request.IdentityId, false, request.RecoveryMethod,
                0.8, true, "Verification evidence is missing", evaluatedAt);
        }

        // 4. Evaluate recovery method risk level
        var riskScore = EvaluateMethodRisk(request.RecoveryMethod);

        // 5. Evaluate recovery eligibility — guardian requires additional verification
        if (request.RecoveryMethod == RecoveryMethod.GuardianVerification)
        {
            return new IdentityRecoveryResult(
                recoveryId, request.IdentityId, false, request.RecoveryMethod,
                riskScore, true, "Guardian verification requires additional approval", evaluatedAt);
        }

        // 6. Determine recovery approval
        var approved = riskScore < 0.7;
        var reason = approved ? "Recovery approved" : "Risk score too high";

        return new IdentityRecoveryResult(
            recoveryId, request.IdentityId, approved, request.RecoveryMethod,
            riskScore, !approved, reason, evaluatedAt);
    }

    private static double EvaluateMethodRisk(RecoveryMethod method)
    {
        return method switch
        {
            RecoveryMethod.EmailVerification => 0.2,
            RecoveryMethod.PhoneVerification => 0.3,
            RecoveryMethod.DocumentVerification => 0.4,
            RecoveryMethod.MultiFactorRecovery => 0.1,
            RecoveryMethod.GuardianVerification => 0.5,
            _ => 1.0
        };
    }

    public static string ComputeRecoveryId(Guid identityId, DateTime requestTime, RecoveryMethod method)
    {
        var input = $"{identityId}{requestTime:O}{method}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
