namespace Whycespace.Engines.T0U.Governance;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.Governance.Evidence.Models;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class GovernanceEvidenceRecorder
{
    private readonly ChainEvidenceGateway _gateway;
    private const string Domain = "governance";

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public GovernanceEvidenceRecorder(ChainEvidenceGateway gateway)
    {
        _gateway = gateway;
    }

    public GovernanceEvidenceResult Execute(RecordGovernanceEvidenceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProposalId == Guid.Empty)
            return Failure(command, "Proposal ID must not be empty.");

        if (command.EventReferenceId == Guid.Empty)
            return Failure(command, "Event reference ID must not be empty.");

        if (!Enum.IsDefined(command.EvidenceType))
            return Failure(command, $"Invalid evidence type: {command.EvidenceType}.");

        if (string.IsNullOrWhiteSpace(command.EvidencePayload))
            return Failure(command, "Evidence payload must not be empty.");

        if (command.RecordedByGuardianId == Guid.Empty)
            return Failure(command, "Recorded-by guardian ID must not be empty.");

        var evidenceId = Guid.NewGuid();
        var evidenceHash = ComputeEvidenceHash(command);
        var merkleRoot = ComputeMerkleRoot(evidenceHash, command);

        _gateway.SubmitEvidence(
            $"gov-evidence-{evidenceId}",
            Domain,
            command.EvidenceType.ToString(),
            new { command.ProposalId, command.EventReferenceId, command.EvidencePayload, command.EvidenceType });

        return new GovernanceEvidenceResult(
            Success: true,
            EvidenceId: evidenceId,
            ProposalId: command.ProposalId,
            EvidenceType: command.EvidenceType,
            EvidenceHash: evidenceHash,
            MerkleRoot: merkleRoot,
            Message: "Governance evidence recorded successfully.",
            ExecutedAt: DateTime.UtcNow);
    }

    public ChainLedgerEntry RecordProposal(GovernanceProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return _gateway.SubmitEvidence(
            $"gov-proposal-{proposal.ProposalId}",
            Domain,
            "ProposalCreated",
            proposal);
    }

    public ChainLedgerEntry RecordVote(GovernanceVote vote)
    {
        ArgumentNullException.ThrowIfNull(vote);
        return _gateway.SubmitEvidence(
            $"gov-vote-{vote.VoteId}",
            Domain,
            "VoteCast",
            vote);
    }

    public ChainLedgerEntry RecordDecision(GovernanceDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return _gateway.SubmitEvidence(
            $"gov-decision-{decision.ProposalId}",
            Domain,
            "DecisionMade",
            decision);
    }

    private static string ComputeEvidenceHash(RecordGovernanceEvidenceCommand command)
    {
        var canonical = new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["evidencePayload"] = command.EvidencePayload,
            ["evidenceType"] = command.EvidenceType.ToString(),
            ["eventReferenceId"] = command.EventReferenceId.ToString(),
            ["proposalId"] = command.ProposalId.ToString(),
            ["recordedByGuardianId"] = command.RecordedByGuardianId.ToString(),
            ["timestamp"] = command.Timestamp.ToString("O")
        };

        var json = JsonSerializer.Serialize(canonical, CanonicalJsonOptions);
        return ComputeHash(json);
    }

    private static string ComputeMerkleRoot(string evidenceHash, RecordGovernanceEvidenceCommand command)
    {
        var leaves = new[]
        {
            evidenceHash,
            ComputeHash(command.ProposalId.ToString()),
            ComputeHash(command.EventReferenceId.ToString()),
            ComputeHash(command.EvidenceType.ToString()),
            ComputeHash(command.EvidencePayload)
        };

        var level = leaves.ToList();

        while (level.Count > 1)
        {
            var next = new List<string>();
            for (var i = 0; i < level.Count; i += 2)
            {
                var left = level[i];
                var right = i + 1 < level.Count ? level[i + 1] : left;
                var combined = string.CompareOrdinal(left, right) <= 0 ? left + right : right + left;
                next.Add(ComputeHash(combined));
            }
            level = next;
        }

        return level[0];
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    private static GovernanceEvidenceResult Failure(RecordGovernanceEvidenceCommand command, string message)
    {
        return new GovernanceEvidenceResult(
            Success: false,
            EvidenceId: Guid.Empty,
            ProposalId: command.ProposalId,
            EvidenceType: command.EvidenceType,
            EvidenceHash: string.Empty,
            MerkleRoot: string.Empty,
            Message: message,
            ExecutedAt: DateTime.UtcNow);
    }
}
