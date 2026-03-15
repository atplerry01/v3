namespace Whycespace.Engines.T0U.WhyceID.Revocation;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

public sealed class IdentityRevocationEvaluationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRevocationStore _revocationStore;

    private const int GovernanceReviewThreshold = 70;

    public IdentityRevocationEvaluationEngine(
        IdentityRegistry registry,
        IdentityRevocationStore revocationStore)
    {
        _registry = registry;
        _revocationStore = revocationStore;
    }

    public IdentityRevocationResult Evaluate(IdentityRevocationRequest request)
    {
        var evaluatedAt = DateTime.UtcNow;
        var revocationId = ComputeRevocationId(
            request.IdentityId, request.RequestedAt, request.RevocationScope);

        // Step 1: Validate identity existence
        if (!_registry.Exists(request.IdentityId))
        {
            return Rejected(revocationId, request, evaluatedAt,
                "Identity does not exist.", riskScore: 0);
        }

        var identity = _registry.Get(request.IdentityId);

        // Step 2: Validate current identity status
        if (identity.Status == IdentityStatus.Revoked)
        {
            return Rejected(revocationId, request, evaluatedAt,
                "Identity is already revoked.", riskScore: 0);
        }

        // Step 3: Evaluate revocation scope
        if (request.RevocationScope == RevocationScope.FullIdentityRevocation
            && identity.Status == IdentityStatus.Pending)
        {
            return Rejected(revocationId, request, evaluatedAt,
                "Pending identities cannot undergo full revocation.", riskScore: 10);
        }

        // Step 4: Evaluate revocation reason validity
        if (!Enum.IsDefined(request.RevocationReason))
        {
            return Rejected(revocationId, request, evaluatedAt,
                "Invalid revocation reason.", riskScore: 0);
        }

        // Step 5: Evaluate evidence validity
        if (string.IsNullOrWhiteSpace(request.Evidence))
        {
            return Rejected(revocationId, request, evaluatedAt,
                "Evidence is required for revocation evaluation.", riskScore: 0);
        }

        // Step 6: Compute risk score
        var riskScore = ComputeRiskScore(request, identity);

        // Step 7: Evaluate governance override conditions
        var requiresGovernanceReview = riskScore >= GovernanceReviewThreshold
            || request.RevocationScope == RevocationScope.FullIdentityRevocation
            || request.RevocationReason == RevocationReason.RegulatorySuspension;

        // Step 8: Determine approval
        var approved = !requiresGovernanceReview || riskScore >= 90;
        var reason = approved
            ? "Revocation approved based on evaluation criteria."
            : requiresGovernanceReview
                ? "Revocation requires governance review before approval."
                : "Revocation not approved based on evaluation criteria.";

        return new IdentityRevocationResult(
            revocationId,
            request.IdentityId,
            approved,
            request.RevocationScope,
            request.RevocationReason,
            riskScore,
            requiresGovernanceReview,
            reason,
            evaluatedAt);
    }

    private static int ComputeRiskScore(
        IdentityRevocationRequest request,
        IdentityAggregate identity)
    {
        var score = 0;

        // Reason-based scoring
        score += request.RevocationReason switch
        {
            RevocationReason.CompromisedAccount => 40,
            RevocationReason.FraudDetected => 35,
            RevocationReason.RegulatorySuspension => 30,
            RevocationReason.PolicyViolation => 20,
            RevocationReason.ManualGovernanceAction => 15,
            _ => 0
        };

        // Scope-based scoring
        score += request.RevocationScope switch
        {
            RevocationScope.FullIdentityRevocation => 30,
            RevocationScope.CredentialRevocation => 20,
            RevocationScope.DeviceRevocation => 15,
            RevocationScope.SessionRevocation => 10,
            RevocationScope.RoleRevocation => 10,
            RevocationScope.PermissionRevocation => 5,
            _ => 0
        };

        // Status-based scoring
        if (identity.Status == IdentityStatus.Verified)
            score += 15;
        else if (identity.Status == IdentityStatus.Suspended)
            score += 10;

        // Evidence length as a basic quality indicator
        if (request.Evidence.Length > 100)
            score += 10;
        else if (request.Evidence.Length > 50)
            score += 5;

        return Math.Min(score, 100);
    }

    public static string ComputeRevocationId(
        Guid identityId, DateTime requestedAt, RevocationScope scope)
    {
        var input = $"{identityId}{requestedAt:O}{scope}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }

    private static IdentityRevocationResult Rejected(
        string revocationId,
        IdentityRevocationRequest request,
        DateTime evaluatedAt,
        string reason,
        int riskScore)
    {
        return new IdentityRevocationResult(
            revocationId,
            request.IdentityId,
            Approved: false,
            request.RevocationScope,
            request.RevocationReason,
            riskScore,
            RequiresGovernanceReview: false,
            reason,
            evaluatedAt);
    }
}
