using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityGraphEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityGraphStore _store;
    private readonly IdentityGraphEngine _engine;

    public IdentityGraphEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityGraphStore();
        _engine = new IdentityGraphEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void CreateRelationship_ValidIdentity_ReturnsEdge()
    {
        var sourceId = RegisterIdentity();
        var targetId = Guid.NewGuid();

        var edge = _engine.CreateRelationship(sourceId, targetId, "operator");

        Assert.NotEqual(Guid.Empty, edge.EdgeId);
        Assert.Equal(sourceId, edge.SourceIdentityId);
        Assert.Equal(targetId, edge.TargetEntityId);
        Assert.Equal("operator", edge.Relationship);
    }

    [Fact]
    public void CreateRelationship_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _engine.CreateRelationship(missingId, targetId, "guardian"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void CreateRelationship_EmptyRelationship_Throws()
    {
        var sourceId = RegisterIdentity();
        var targetId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () => _engine.CreateRelationship(sourceId, targetId, ""));
    }

    [Fact]
    public void GetRelationships_ReturnsBySource()
    {
        var sourceId = RegisterIdentity();
        var target1 = Guid.NewGuid();
        var target2 = Guid.NewGuid();

        _engine.CreateRelationship(sourceId, target1, "operator");
        _engine.CreateRelationship(sourceId, target2, "guardian");

        var relationships = _engine.GetRelationships(sourceId);

        Assert.Equal(2, relationships.Count);
    }

    [Fact]
    public void GetRelationshipsByType_ReturnsMatchingType()
    {
        var source1 = RegisterIdentity();
        var source2 = RegisterIdentity();

        _engine.CreateRelationship(source1, Guid.NewGuid(), "operator");
        _engine.CreateRelationship(source2, Guid.NewGuid(), "operator");
        _engine.CreateRelationship(source1, Guid.NewGuid(), "guardian");

        var operators = _engine.GetRelationshipsByType("operator");

        Assert.Equal(2, operators.Count);
        Assert.All(operators, e => Assert.Equal("operator", e.Relationship));
    }

    [Fact]
    public void RemoveRelationship_EdgesRemoved()
    {
        var sourceId = RegisterIdentity();
        var edge = _engine.CreateRelationship(sourceId, Guid.NewGuid(), "cluster_admin");

        _engine.RemoveRelationship(edge.EdgeId);

        var relationships = _engine.GetRelationships(sourceId);
        Assert.Empty(relationships);
    }

    [Fact]
    public void MultipleRelationships_SupportedPerIdentity()
    {
        var sourceId = RegisterIdentity();

        _engine.CreateRelationship(sourceId, Guid.NewGuid(), "operator");
        _engine.CreateRelationship(sourceId, Guid.NewGuid(), "guardian");
        _engine.CreateRelationship(sourceId, Guid.NewGuid(), "spv_operator");
        _engine.CreateRelationship(sourceId, Guid.NewGuid(), "service_owner");

        var relationships = _engine.GetRelationships(sourceId);
        Assert.Equal(4, relationships.Count);
    }
}
