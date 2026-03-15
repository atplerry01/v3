using Whycespace.Engines.T0U.WhyceID.Recovery;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRecoveryEvaluationEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRecoveryEvaluationEngine _engine;

    public IdentityRecoveryEvaluationEngineTests()
    {
        _registry = new IdentityRegistry();
        _engine = new IdentityRecoveryEvaluationEngine(_registry);
    }

    private Guid RegisterIdentity(bool verify = true)
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        if (verify) identity.Verify();
        _registry.Register(identity);
        return id.Value;
    }

    private Guid RegisterRevokedIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        identity.Verify();
        identity.Revoke();
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void Evaluate_ApprovedWithValidVerification()
    {
        var identityId = RegisterIdentity();
        var request = new IdentityRecoveryRequest(
            identityId,
            DateTime.UtcNow,
            RecoveryMethod.EmailVerification,
            "valid-evidence-token",
            "device-fp-123",
            "192.168.1.1",
            "US-East");

        var result = _engine.Evaluate(request);

        Assert.True(result.Approved);
        Assert.Equal(identityId, result.IdentityId);
        Assert.Equal(RecoveryMethod.EmailVerification, result.RecoveryMethod);
        Assert.Equal(0.2, result.RiskScore);
        Assert.False(result.RequiresAdditionalVerification);
        Assert.Equal("Recovery approved", result.Reason);
    }

    [Fact]
    public void Evaluate_RejectedForNonExistentIdentity()
    {
        var request = new IdentityRecoveryRequest(
            Guid.NewGuid(),
            DateTime.UtcNow,
            RecoveryMethod.EmailVerification,
            "evidence",
            "device-fp",
            "192.168.1.1",
            "US-East");

        var result = _engine.Evaluate(request);

        Assert.False(result.Approved);
        Assert.Equal("Identity does not exist", result.Reason);
    }

    [Fact]
    public void Evaluate_RejectedForRevokedIdentity()
    {
        var identityId = RegisterRevokedIdentity();
        var request = new IdentityRecoveryRequest(
            identityId,
            DateTime.UtcNow,
            RecoveryMethod.PhoneVerification,
            "evidence",
            "device-fp",
            "192.168.1.1",
            "US-East");

        var result = _engine.Evaluate(request);

        Assert.False(result.Approved);
        Assert.Equal("Identity has been revoked", result.Reason);
        Assert.Equal(1.0, result.RiskScore);
    }

    [Fact]
    public void Evaluate_RequiresAdditionalVerification_MissingEvidence()
    {
        var identityId = RegisterIdentity();
        var request = new IdentityRecoveryRequest(
            identityId,
            DateTime.UtcNow,
            RecoveryMethod.EmailVerification,
            "",
            "device-fp",
            "192.168.1.1",
            "US-East");

        var result = _engine.Evaluate(request);

        Assert.False(result.Approved);
        Assert.True(result.RequiresAdditionalVerification);
        Assert.Equal("Verification evidence is missing", result.Reason);
    }

    [Fact]
    public void Evaluate_GuardianRecovery_RequiresAdditionalApproval()
    {
        var identityId = RegisterIdentity();
        var request = new IdentityRecoveryRequest(
            identityId,
            DateTime.UtcNow,
            RecoveryMethod.GuardianVerification,
            "guardian-evidence",
            "device-fp",
            "192.168.1.1",
            "US-East");

        var result = _engine.Evaluate(request);

        Assert.False(result.Approved);
        Assert.True(result.RequiresAdditionalVerification);
        Assert.Equal(0.5, result.RiskScore);
        Assert.Equal("Guardian verification requires additional approval", result.Reason);
    }

    [Fact]
    public void Evaluate_DeterministicRecoveryId()
    {
        var identityId = RegisterIdentity();
        var requestedAt = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);

        var request1 = new IdentityRecoveryRequest(
            identityId, requestedAt, RecoveryMethod.EmailVerification,
            "evidence", "device-fp", "192.168.1.1", "US-East");

        var request2 = new IdentityRecoveryRequest(
            identityId, requestedAt, RecoveryMethod.EmailVerification,
            "different-evidence", "other-device", "10.0.0.1", "EU-West");

        var result1 = _engine.Evaluate(request1);
        var result2 = _engine.Evaluate(request2);

        Assert.Equal(result1.RecoveryId, result2.RecoveryId);
        Assert.NotEmpty(result1.RecoveryId);
    }

    [Fact]
    public void Evaluate_DifferentInputs_DifferentRecoveryId()
    {
        var identityId = RegisterIdentity();
        var now = DateTime.UtcNow;

        var request1 = new IdentityRecoveryRequest(
            identityId, now, RecoveryMethod.EmailVerification,
            "evidence", "device-fp", "192.168.1.1", "US-East");

        var request2 = new IdentityRecoveryRequest(
            identityId, now, RecoveryMethod.PhoneVerification,
            "evidence", "device-fp", "192.168.1.1", "US-East");

        var result1 = _engine.Evaluate(request1);
        var result2 = _engine.Evaluate(request2);

        Assert.NotEqual(result1.RecoveryId, result2.RecoveryId);
    }

    [Fact]
    public async Task Evaluate_ConcurrentRecoveryEvaluationSafety()
    {
        var identityId = RegisterIdentity();

        var tasks = Enumerable.Range(0, 100).Select(_ =>
        {
            return Task.Run(() =>
            {
                var request = new IdentityRecoveryRequest(
                    identityId,
                    DateTime.UtcNow,
                    RecoveryMethod.MultiFactorRecovery,
                    "evidence",
                    "device-fp",
                    "192.168.1.1",
                    "US-East");

                return _engine.Evaluate(request);
            });
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Assert.True(result.Approved);
            Assert.Equal(identityId, result.IdentityId);
        }
    }

    [Fact]
    public void ComputeRecoveryId_IsSha256()
    {
        var id = Guid.NewGuid();
        var time = DateTime.UtcNow;
        var method = RecoveryMethod.EmailVerification;

        var recoveryId = IdentityRecoveryEvaluationEngine.ComputeRecoveryId(id, time, method);

        // SHA256 hex string is 64 characters
        Assert.Equal(64, recoveryId.Length);
        Assert.Matches("^[0-9a-f]{64}$", recoveryId);
    }
}
