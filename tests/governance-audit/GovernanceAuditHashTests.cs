using Whycespace.Engines.T3I.Reporting.Governance;
using Whycespace.Engines.T3I.Reporting.Governance.Commands;

namespace Whycespace.GovernanceAudit.Tests;

public class GovernanceAuditHashTests
{
    [Fact]
    public void GenerateAuditHash_SameInputs_ReturnsSameHash()
    {
        var proposalId = Guid.NewGuid();
        var actionType = GovernanceAuditActionType.VoteCast;
        var performedBy = Guid.NewGuid();
        var actionReferenceId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var hash1 = GovernanceAuditHashGenerator.GenerateAuditHash(
            proposalId, actionType, performedBy, actionReferenceId, timestamp);
        var hash2 = GovernanceAuditHashGenerator.GenerateAuditHash(
            proposalId, actionType, performedBy, actionReferenceId, timestamp);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateAuditHash_DifferentInputs_ReturnsDifferentHash()
    {
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var hash1 = GovernanceAuditHashGenerator.GenerateAuditHash(
            Guid.NewGuid(), GovernanceAuditActionType.VoteCast,
            Guid.NewGuid(), Guid.NewGuid(), timestamp);
        var hash2 = GovernanceAuditHashGenerator.GenerateAuditHash(
            Guid.NewGuid(), GovernanceAuditActionType.DecisionApproved,
            Guid.NewGuid(), Guid.NewGuid(), timestamp);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateAuditHash_ReturnsValidSha256HexString()
    {
        var hash = GovernanceAuditHashGenerator.GenerateAuditHash(
            Guid.NewGuid(), GovernanceAuditActionType.ProposalCreated,
            Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void GenerateAuditId_SameInputs_ReturnsSameId()
    {
        var proposalId = Guid.NewGuid();
        var actionType = GovernanceAuditActionType.DisputeRaised;
        var performedBy = Guid.NewGuid();
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var id1 = GovernanceAuditHashGenerator.GenerateAuditId(
            proposalId, actionType, performedBy, timestamp);
        var id2 = GovernanceAuditHashGenerator.GenerateAuditId(
            proposalId, actionType, performedBy, timestamp);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateAuditId_DifferentFromAuditHash()
    {
        var proposalId = Guid.NewGuid();
        var actionType = GovernanceAuditActionType.ProposalCreated;
        var performedBy = Guid.NewGuid();
        var actionReferenceId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var auditId = GovernanceAuditHashGenerator.GenerateAuditId(
            proposalId, actionType, performedBy, timestamp);
        var auditHash = GovernanceAuditHashGenerator.GenerateAuditHash(
            proposalId, actionType, performedBy, actionReferenceId, timestamp);

        Assert.NotEqual(auditId, auditHash);
    }
}
