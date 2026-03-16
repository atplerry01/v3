namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Models;
using ChainHashUtility = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainHashUtility;

public sealed class ChainAuditEngine
{
    public ChainAuditResult Execute(ChainAuditCommand command)
    {
        var blocks = command.Blocks;
        var entries = command.LedgerEntries;

        var sorted = blocks.OrderBy(b => b.BlockNumber).ToList();

        var brokenLinks = 0;
        var invalidHashes = 0;
        var merkleRootMismatches = 0;
        var sequenceGaps = 0;

        // Build entry lookup by EntryId for Merkle root recomputation
        var entryById = new Dictionary<string, ChainLedgerEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
            entryById[entry.EntryId] = entry;

        // Block link integrity and block hash validation
        for (var i = 0; i < sorted.Count; i++)
        {
            var block = sorted[i];

            // Block height progression: expect sequential heights
            if (i == 0)
            {
                var expectedHeight = command.SnapshotHeight.HasValue
                    ? command.SnapshotHeight.Value + 1
                    : 0;

                if (block.BlockNumber != expectedHeight)
                    sequenceGaps++;

                // Genesis block linkage
                if (block.BlockNumber == 0 && block.PreviousBlockHash != "genesis")
                    brokenLinks++;
            }
            else
            {
                var previous = sorted[i - 1];

                // Height continuity
                if (block.BlockNumber != previous.BlockNumber + 1)
                    sequenceGaps++;

                // Previous block hash linkage
                if (block.PreviousBlockHash != previous.BlockHash)
                    brokenLinks++;
            }

            // Recompute block hash
            var recomputedHash = ChainHashUtility.GenerateBlockHash(
                Guid.Parse(block.BlockId),
                block.BlockNumber,
                block.MerkleRoot,
                block.PreviousBlockHash,
                block.EntryIds.Count,
                block.Timestamp);

            if (!string.Equals(recomputedHash, block.BlockHash, StringComparison.Ordinal))
                invalidHashes++;

            // Recompute Merkle root from entry payload hashes
            var leafHashes = new List<string>();
            foreach (var entryId in block.EntryIds)
            {
                if (entryById.TryGetValue(entryId, out var entry))
                    leafHashes.Add(entry.PayloadHash);
            }

            if (leafHashes.Count > 0)
            {
                var recomputedMerkle = ChainHashUtility.ComputeMerkleRoot(leafHashes);
                if (!string.Equals(recomputedMerkle, block.MerkleRoot, StringComparison.Ordinal))
                    merkleRootMismatches++;
            }
        }

        // Ledger sequence integrity: verify entry ordering continuity
        var orderedEntries = entries.OrderBy(e => e.Timestamp).ToList();
        for (var i = 1; i < orderedEntries.Count; i++)
        {
            var current = orderedEntries[i];
            var previous = orderedEntries[i - 1];

            if (!string.Equals(current.PreviousHash, previous.PayloadHash, StringComparison.Ordinal))
                sequenceGaps++;
        }

        var anomalyDetected = brokenLinks > 0
            || invalidHashes > 0
            || merkleRootMismatches > 0
            || sequenceGaps > 0;

        return new ChainAuditResult(
            sorted.Count,
            entries.Count,
            brokenLinks,
            invalidHashes,
            merkleRootMismatches,
            sequenceGaps,
            anomalyDetected,
            command.Timestamp,
            command.TraceId);
    }
}
