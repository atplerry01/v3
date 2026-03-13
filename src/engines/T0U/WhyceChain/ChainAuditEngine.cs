namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

public sealed class ChainAuditEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerStore _ledgerStore;
    private readonly IntegrityVerificationEngine _integrityEngine;

    public ChainAuditEngine(
        ChainBlockStore blockStore,
        ChainLedgerStore ledgerStore,
        IntegrityVerificationEngine integrityEngine)
    {
        _blockStore = blockStore;
        _ledgerStore = ledgerStore;
        _integrityEngine = integrityEngine;
    }

    public ChainAuditResult AuditChain()
    {
        var issues = new List<string>();
        var latest = _blockStore.GetLatestBlock();

        if (latest is null)
            return new ChainAuditResult(true, 0, 0, issues, DateTimeOffset.UtcNow);

        var totalEntries = 0;

        for (long i = 0; i <= latest.BlockNumber; i++)
        {
            var blockResult = AuditBlock(i);
            totalEntries += blockResult.EntriesAudited;

            if (!blockResult.Valid)
                issues.AddRange(blockResult.Issues);
        }

        return new ChainAuditResult(
            issues.Count == 0,
            (int)(latest.BlockNumber + 1),
            totalEntries,
            issues,
            DateTimeOffset.UtcNow);
    }

    public ChainAuditResult AuditBlock(long blockNumber)
    {
        var issues = new List<string>();

        try
        {
            var block = _blockStore.GetBlock(blockNumber);

            if (!_integrityEngine.VerifyBlock(blockNumber))
                issues.Add($"Block {blockNumber}: Merkle root mismatch");

            if (blockNumber == 0 && block.PreviousBlockHash != "genesis")
                issues.Add($"Block 0: invalid genesis reference");

            if (blockNumber > 0)
            {
                var previous = _blockStore.GetBlock(blockNumber - 1);
                if (block.PreviousBlockHash != previous.BlockHash)
                    issues.Add($"Block {blockNumber}: previous hash mismatch");
            }

            foreach (var entryId in block.EntryIds)
            {
                if (!_integrityEngine.VerifyEntry(entryId))
                    issues.Add($"Block {blockNumber}: missing entry {entryId}");
            }

            return new ChainAuditResult(
                issues.Count == 0,
                1,
                block.EntryIds.Count,
                issues,
                DateTimeOffset.UtcNow);
        }
        catch (KeyNotFoundException)
        {
            issues.Add($"Block {blockNumber}: not found");
            return new ChainAuditResult(false, 0, 0, issues, DateTimeOffset.UtcNow);
        }
    }

    public ChainAuditResult AuditEvent(string entryId)
    {
        var issues = new List<string>();

        if (!_integrityEngine.VerifyEntry(entryId))
        {
            issues.Add($"Entry {entryId}: not found in ledger");
            return new ChainAuditResult(false, 0, 0, issues, DateTimeOffset.UtcNow);
        }

        var entry = _ledgerStore.GetEntry(entryId);

        if (entry.BlockId is null)
            issues.Add($"Entry {entryId}: not yet anchored to a block");

        return new ChainAuditResult(
            issues.Count == 0,
            entry.BlockId is not null ? 1 : 0,
            1,
            issues,
            DateTimeOffset.UtcNow);
    }
}
