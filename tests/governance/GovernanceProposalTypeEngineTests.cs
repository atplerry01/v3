using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

namespace Whycespace.Governance.Tests;

public class GovernanceProposalTypeEngineTests
{
    private readonly GovernanceProposalTypeStore _typeStore = new();
    private readonly GovernanceProposalTypeEngine _engine;

    public GovernanceProposalTypeEngineTests()
    {
        _engine = new GovernanceProposalTypeEngine(_typeStore);
    }

    [Fact]
    public void CreateType_Succeeds()
    {
        var type = _engine.CreateType("policy-change", "PolicyChange", "Changes to governance policies");

        Assert.Equal("policy-change", type.TypeId);
        Assert.Equal("PolicyChange", type.Name);
        Assert.Equal("Changes to governance policies", type.Description);
    }

    [Fact]
    public void CreateType_Duplicate_Throws()
    {
        _engine.CreateType("dup", "Dup", "Desc");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateType("dup", "Dup2", "Desc2"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void CreateType_EmptyId_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateType("", "Name", "Desc"));
        Assert.Contains("Type ID is required", ex.Message);
    }

    [Fact]
    public void CreateType_EmptyName_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateType("id", "", "Desc"));
        Assert.Contains("Type name is required", ex.Message);
    }

    [Fact]
    public void GetType_Succeeds()
    {
        _engine.CreateType("system-upgrade", "SystemUpgrade", "System upgrades");

        var type = _engine.GetType("system-upgrade");

        Assert.Equal("SystemUpgrade", type.Name);
    }

    [Fact]
    public void GetType_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetType("nonexistent"));
        Assert.Contains("Proposal type not found", ex.Message);
    }

    [Fact]
    public void ListTypes_ReturnsAll()
    {
        _engine.CreateType("policy-change", "PolicyChange", "Policy changes");
        _engine.CreateType("system-upgrade", "SystemUpgrade", "System upgrades");
        _engine.CreateType("emergency-action", "EmergencyAction", "Emergency actions");
        _engine.CreateType("dispute-resolution", "DisputeResolution", "Dispute resolutions");

        var types = _engine.ListTypes();

        Assert.Equal(4, types.Count);
    }

    [Fact]
    public void ListTypes_Empty_ReturnsEmpty()
    {
        var types = _engine.ListTypes();

        Assert.Empty(types);
    }
}
