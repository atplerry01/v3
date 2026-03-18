namespace Whycespace.Systems.Downstream.Spv.Governance;

public sealed record VotingModel(
    Guid VoteId,
    Guid SpvId,
    Guid ProposalId,
    Guid VoterIdentityId,
    string VoteDirection,
    decimal VotingWeight,
    DateTimeOffset CastAt
);

public sealed record VotingResult(
    Guid ProposalId,
    Guid SpvId,
    decimal ForWeight,
    decimal AgainstWeight,
    decimal AbstainWeight,
    bool QuorumReached,
    bool Passed,
    DateTimeOffset TalliedAt
);
