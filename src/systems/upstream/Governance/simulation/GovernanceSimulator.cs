namespace Whycespace.Systems.Upstream.Governance.Simulation;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceSimulationRequest(
    string ProposalId,
    string ProposalType,
    int SimulatedVoterCount,
    IReadOnlyDictionary<string, object> Parameters);

public sealed record GovernanceSimulationResult(
    string ProposalId,
    bool QuorumReached,
    string ProjectedOutcome,
    int SimulatedApprove,
    int SimulatedReject,
    int SimulatedAbstain,
    bool IsDryRun,
    DateTimeOffset SimulatedAt);

public sealed class GovernanceSimulator
{
    public GovernanceSimulationResult Simulate(GovernanceSimulationRequest request)
    {
        var approve = request.SimulatedVoterCount / 2 + 1;
        var reject = request.SimulatedVoterCount - approve;
        var quorumThreshold = (int)(request.SimulatedVoterCount * 0.5);
        var quorumReached = (approve + reject) >= quorumThreshold;
        var outcome = approve > reject && quorumReached ? "Approved" : "Rejected";

        return new GovernanceSimulationResult(
            request.ProposalId,
            quorumReached,
            outcome,
            approve,
            reject,
            SimulatedAbstain: 0,
            IsDryRun: true,
            DateTimeOffset.UtcNow);
    }
}
