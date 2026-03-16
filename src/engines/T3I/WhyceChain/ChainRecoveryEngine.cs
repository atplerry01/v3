namespace Whycespace.Engines.T3I.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;

public sealed class ChainRecoveryEngine
{
    public ChainRecoveryResult Execute(ChainRecoveryCommand command)
    {
        ValidateSnapshotIntegrity(command);
        ValidateReplicatedBlockLinkage(command);

        var recoveredBlocks = ReconstructChainBlocks(command);
        var recoveredEntries = RebuildLedgerEntries(command, recoveredBlocks);
        var recoveredHeight = recoveredBlocks.Count > 0
            ? recoveredBlocks[^1].BlockHeight
            : command.SnapshotHeight;
        var recoveredEntryCount = recoveredEntries.Count;
        var latestBlockHash = recoveredBlocks.Count > 0
            ? recoveredBlocks[^1].BlockHash
            : command.SnapshotBlockHash;
        var recoveryHash = ComputeRecoveryHash(recoveredHeight, recoveredEntryCount, latestBlockHash);

        return new ChainRecoveryResult(
            recoveredBlocks,
            recoveredEntries,
            recoveredHeight,
            recoveryHash,
            recoveredEntryCount,
            command.Timestamp,
            command.TraceId);
    }

    private static void ValidateSnapshotIntegrity(ChainRecoveryCommand command)
    {
        if (command.SnapshotHeight < 0)
            throw new InvalidOperationException("Snapshot height must be non-negative.");

        if (string.IsNullOrWhiteSpace(command.SnapshotBlockHash))
            throw new InvalidOperationException("Snapshot block hash must not be empty.");

        if (command.ReplicatedBlocks.Count == 0)
            return;

        var firstBlock = command.ReplicatedBlocks[0];

        if (firstBlock.BlockHeight != command.SnapshotHeight + 1)
            throw new InvalidOperationException(
                $"First replicated block height {firstBlock.BlockHeight} does not start at snapshot height {command.SnapshotHeight} + 1.");

        if (firstBlock.PreviousBlockHash != command.SnapshotBlockHash)
            throw new InvalidOperationException(
                $"First replicated block previous hash does not match snapshot block hash.");
    }

    private static void ValidateReplicatedBlockLinkage(ChainRecoveryCommand command)
    {
        for (var i = 1; i < command.ReplicatedBlocks.Count; i++)
        {
            var previous = command.ReplicatedBlocks[i - 1];
            var current = command.ReplicatedBlocks[i];

            if (current.BlockHeight != previous.BlockHeight + 1)
                throw new InvalidOperationException(
                    $"Block at index {i} has height {current.BlockHeight}, expected {previous.BlockHeight + 1}.");

            if (current.PreviousBlockHash != previous.BlockHash)
                throw new InvalidOperationException(
                    $"Block at index {i} previous hash does not match preceding block hash.");
        }
    }

    private static List<ChainBlock> ReconstructChainBlocks(ChainRecoveryCommand command)
    {
        var recovered = new List<ChainBlock>(command.ReplicatedBlocks.Count);

        foreach (var block in command.ReplicatedBlocks)
        {
            var expectedHash = ChainHashUtility.ComputeBlockHash(
                block.BlockHeight,
                block.PreviousBlockHash,
                block.MerkleRoot,
                block.EntryCount,
                block.CreatedAt);

            if (expectedHash != block.BlockHash)
                throw new InvalidOperationException(
                    $"Block at height {block.BlockHeight} has invalid hash. Expected {expectedHash}, got {block.BlockHash}.");

            recovered.Add(block);
        }

        return recovered;
    }

    private static List<ChainLedgerEntry> RebuildLedgerEntries(
        ChainRecoveryCommand command,
        IReadOnlyList<ChainBlock> recoveredBlocks)
    {
        var blockEntryIds = new HashSet<Guid>();
        foreach (var block in recoveredBlocks)
        {
            foreach (var entry in block.Entries)
                blockEntryIds.Add(entry.EntryId);
        }

        var recovered = new List<ChainLedgerEntry>();

        foreach (var entry in command.ReplicatedLedgerEntries)
        {
            if (blockEntryIds.Contains(entry.EntryId))
                recovered.Add(entry);
        }

        recovered.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));

        return recovered;
    }

    private static string ComputeRecoveryHash(long recoveredHeight, int recoveredEntryCount, string latestBlockHash)
    {
        var input = $"{recoveredHeight}:{recoveredEntryCount}:{latestBlockHash}";
        return Convert.ToHexString(
            global::System.Security.Cryptography.SHA256.HashData(
                global::System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
