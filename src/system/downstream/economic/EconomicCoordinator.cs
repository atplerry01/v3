namespace Whycespace.System.Downstream.Economic;

public sealed record EconomicTransaction(
    Guid TransactionId,
    string TransactionType,
    Guid SpvId,
    decimal Amount,
    DateTimeOffset Timestamp
);

public sealed class EconomicCoordinator
{
    private readonly List<EconomicTransaction> _transactions = new();

    public void RecordTransaction(EconomicTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    public IReadOnlyList<EconomicTransaction> GetTransactions(Guid? spvId = null)
    {
        return spvId is null
            ? _transactions
            : _transactions.Where(t => t.SpvId == spvId.Value).ToList();
    }

    public decimal GetNetPosition(Guid spvId)
        => _transactions.Where(t => t.SpvId == spvId).Sum(t => t.Amount);
}
