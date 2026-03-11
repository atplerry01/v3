namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceProposalTypeEngine
{
    private readonly GovernanceProposalTypeStore _typeStore;

    public GovernanceProposalTypeEngine(GovernanceProposalTypeStore typeStore)
    {
        _typeStore = typeStore;
    }

    public GovernanceProposalType CreateType(string typeId, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            throw new InvalidOperationException("Type ID is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Type name is required.");

        var type = new GovernanceProposalType(typeId, name, description);
        _typeStore.Add(type);
        return type;
    }

    public GovernanceProposalType GetType(string typeId)
    {
        return _typeStore.Get(typeId)
            ?? throw new KeyNotFoundException($"Proposal type not found: {typeId}");
    }

    public IReadOnlyList<GovernanceProposalType> ListTypes()
    {
        return _typeStore.ListAll();
    }
}
