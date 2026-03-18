namespace Whycespace.Engines.T0U.WhyceChain.Block.Builder;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhyceChain.Stores;
using LedgerBlock = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainBlock;
using LedgerEntry = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainLedgerEntry;
using ModelsBlock = Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock;
using ModelsEntry = Whycespace.Systems.Upstream.WhyceChain.Models.ChainLedgerEntry;

/// <summary>
/// Block Builder Engine — constructs ChainBlocks from ordered ledger entries.
/// Stateless, deterministic, thread-safe. No persistence, no event publishing.
/// </summary>
public sealed class BlockBuilderEngine
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockEngine _blockEngine;
    private readonly MerkleProofEngine _merkleEngine;

    public BlockBuilderEngine()
    {
        _ledgerStore = null!;
        _blockEngine = null!;
        _merkleEngine = null!;
    }

    public BlockBuilderEngine(
        ChainLedgerStore ledgerStore,
        ChainBlockEngine blockEngine,
        MerkleProofEngine merkleEngine)
    {
        _ledgerStore = ledgerStore;
        _blockEngine = blockEngine;
        _merkleEngine = merkleEngine;
    }

    public IReadOnlyList<ModelsEntry> CollectPendingEntries()
    {
        return _ledgerStore.GetAllEntries()
            .Where(e => e.BlockId is null)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public ModelsBlock? BuildBlock()
    {
        var pending = CollectPendingEntries();
        if (pending.Count == 0)
            return null;

        var entryIds = pending.Select(e => e.EntryId).ToList();
        var merkleRoot = _merkleEngine.BuildTree(entryIds);
        var block = _blockEngine.CreateBlock(entryIds, merkleRoot);

        foreach (var entry in pending)
        {
            _ledgerStore.UpdateBlockId(entry.EntryId, block.BlockId);
        }

        return block;
    }

    public BlockBuilderResult Execute(BlockBuilderCommand command)
    {
        ValidateEntries(command.LedgerEntries);

        var orderedEntries = command.LedgerEntries
            .OrderBy(e => e.SequenceNumber)
            .ToList();

        var entryHashes = orderedEntries
            .Select(e => e.EntryHash)
            .ToList();

        var merkleRoot = ComputeMerkleRoot(entryHashes);

        var createdAt = command.Timestamp;

        var blockHash = ComputeBlockHash(
            command.BlockHeight,
            command.PreviousBlockHash,
            merkleRoot,
            orderedEntries.Count,
            createdAt);

        var block = new LedgerBlock(
            BlockId: Guid.NewGuid(),
            BlockHeight: command.BlockHeight,
            PreviousBlockHash: command.PreviousBlockHash,
            Entries: orderedEntries,
            MerkleRoot: merkleRoot,
            BlockHash: blockHash,
            EntryCount: orderedEntries.Count,
            CreatedAt: createdAt,
            ValidatorSignature: string.Empty,
            TraceId: command.TraceId);

        return new BlockBuilderResult(
            Block: block,
            MerkleRoot: merkleRoot,
            BlockHash: blockHash,
            EntryCount: orderedEntries.Count,
            GeneratedAt: createdAt,
            TraceId: command.TraceId);
    }

    private static void ValidateEntries(IReadOnlyList<LedgerEntry> entries)
    {
        if (entries is null || entries.Count == 0)
            throw new ArgumentException("Ledger entries must not be empty.", nameof(entries));

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.EntryHash))
                throw new ArgumentException($"Entry {entry.EntryId} has no hash.");
            if (string.IsNullOrEmpty(entry.PayloadHash))
                throw new ArgumentException($"Entry {entry.EntryId} has no payload hash.");
        }
    }

    internal static string ComputeMerkleRoot(IReadOnlyList<string> leafHashes)
    {
        if (leafHashes.Count == 0)
            return ComputeHash("empty");

        var level = leafHashes.Select(ComputeHash).ToList();

        while (level.Count > 1)
        {
            var next = new List<string>();
            for (var i = 0; i < level.Count; i += 2)
            {
                var left = level[i];
                var right = i + 1 < level.Count ? level[i + 1] : left;
                next.Add(ComputeHash(left + right));
            }
            level = next;
        }

        return level[0];
    }

    internal static string ComputeBlockHash(
        long blockHeight,
        string previousBlockHash,
        string merkleRoot,
        int entryCount,
        DateTime createdAt)
    {
        var canonical = $"{blockHeight}|{previousBlockHash}|{merkleRoot}|{entryCount}|{createdAt:O}";
        return ComputeHash(canonical);
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
