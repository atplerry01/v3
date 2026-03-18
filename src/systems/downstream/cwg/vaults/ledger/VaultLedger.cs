namespace Whycespace.Systems.Downstream.Cwg.Vaults.Ledger;

public sealed class VaultLedger : IVaultLedger
{
    private readonly Dictionary<Guid, VaultLedgerEntry> _entries = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _referenceIndex = new();

    public void RecordEntry(VaultLedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.TransactionId == Guid.Empty)
            throw new ArgumentException("TransactionId is required.", nameof(entry));

        if (entry.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required.", nameof(entry));

        if (_entries.ContainsKey(entry.TransactionId))
            throw new InvalidOperationException($"Transaction '{entry.TransactionId}' already exists in the ledger.");

        _entries[entry.TransactionId] = entry;

        if (!_vaultIndex.TryGetValue(entry.VaultId, out var vaultList))
        {
            vaultList = new List<Guid>();
            _vaultIndex[entry.VaultId] = vaultList;
        }
        vaultList.Add(entry.TransactionId);

        if (!_referenceIndex.TryGetValue(entry.ReferenceId, out var refList))
        {
            refList = new List<Guid>();
            _referenceIndex[entry.ReferenceId] = refList;
        }
        refList.Add(entry.TransactionId);
    }

    public VaultLedgerEntry? GetEntry(Guid transactionId)
    {
        _entries.TryGetValue(transactionId, out var entry);
        return entry;
    }

    public IReadOnlyList<VaultLedgerEntry> GetEntriesByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _entries[id]).ToList();
    }

    public IReadOnlyList<VaultLedgerEntry> GetEntriesByReference(Guid referenceId)
    {
        if (!_referenceIndex.TryGetValue(referenceId, out var ids))
            return [];

        return ids.Select(id => _entries[id]).ToList();
    }

    public IReadOnlyList<VaultLedgerEntry> ListEntries() => _entries.Values.ToList();
}
