namespace Whycespace.Engines.T3I.Core.Identity.Models;

public enum GraphNodeType
{
    Identity,
    Device,
    Provider,
    Operator,
    Service
}

public sealed record IdentityGraphNode(
    Guid NodeId,
    GraphNodeType NodeType,
    Guid ReferenceId,
    DateTime CreatedAt);
