namespace Whycespace.Engines.T0U.WhycePolicy.Evaluation;

public sealed record PolicyContextInput(
    Guid IdentityId,
    string ActionType,
    string ResourceType,
    string ResourceId,
    Guid ClusterId,
    Guid SubClusterId,
    Guid SpvId,
    Guid VaultId,
    Dictionary<string, object> Attributes
);
