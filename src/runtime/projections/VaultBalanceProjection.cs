namespace Whycespace.Runtime.Projections;

using Whycespace.Shared.Events;
using Whycespace.Shared.Projections;

public sealed class VaultBalanceProjection : IProjection
{
    private readonly Dictionary<string, decimal> _balances = new();

    public string Name => "VaultBalance";

    public Task HandleAsync(SystemEvent @event)
    {
        var vaultId = @event.AggregateId.ToString();
        switch (@event.EventType)
        {
            case "CapitalAllocated":
                if (@event.Payload.GetValueOrDefault("amount") is decimal allocated)
                    _balances[vaultId] = _balances.GetValueOrDefault(vaultId) - allocated;
                break;
            case "ProfitDistributed":
                if (@event.Payload.GetValueOrDefault("amount") is decimal distributed)
                    _balances[vaultId] = _balances.GetValueOrDefault(vaultId) + distributed;
                break;
        }
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, decimal> GetBalances() => _balances;
}
