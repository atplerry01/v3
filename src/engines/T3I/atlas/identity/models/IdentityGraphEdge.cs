namespace Whycespace.Engines.T3I.Atlas.Identity.Models;

public sealed record IdentityGraphEdge(
    Guid SourceNodeId,
    Guid TargetNodeId,
    string RelationshipType,
    double Strength,
    DateTime CreatedAt);
