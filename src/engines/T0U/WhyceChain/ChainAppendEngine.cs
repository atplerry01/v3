namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Ledger;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;
using LedgerChainBlock = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainBlock;
using ModelsChainBlock = Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock;
using ModelsChainLedgerEntry = Whycespace.Systems.Upstream.WhyceChain.Models.ChainLedgerEntry;

public sealed class ChainAppendEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly BlockBuilderEngine _builderEngine;
    private readonly IntegrityVerificationEngine _integrityEngine;

    public ChainAppendEngine()
    {
        _blockStore = null!;
        _builderEngine = null!;
        _integrityEngine = null!;
    }

    public ChainAppendEngine(
        ChainBlockStore blockStore,
        BlockBuilderEngine builderEngine,
        IntegrityVerificationEngine integrityEngine)
    {
        _blockStore = blockStore;
        _builderEngine = builderEngine;
        _integrityEngine = integrityEngine;
    }

    public ModelsChainBlock? AppendBlock()
    {
        var block = _builderEngine.BuildBlock();
        if (block is null)
            return null;

        if (!ValidateAppend(block))
            throw new InvalidOperationException("Block validation failed after append");

        return block;
    }

    public bool ValidateAppend(ModelsChainBlock block)
    {
        var verifyCommand = new IntegrityVerificationCommand(
            Array.Empty<ModelsChainLedgerEntry>(),
            [block],
            MerkleProof: null,
            TraceId: $"append-verify-{block.BlockNumber}",
            CorrelationId: $"append-{block.BlockNumber}",
            Timestamp: DateTimeOffset.UtcNow);
        var verifyResult = _integrityEngine.Execute(verifyCommand);
        if (!verifyResult.MerkleRootValid)
            return false;

        if (block.BlockNumber == 0)
            return block.PreviousBlockHash == "genesis";

        try
        {
            var previous = _blockStore.GetBlock(block.BlockNumber - 1);
            return block.PreviousBlockHash == previous.BlockHash;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Stateless, deterministic append validation against a ChainAppendCommand.
    /// Does not persist blocks — validation only.
    /// </summary>
    public ChainAppendResult Execute(ChainAppendCommand command)
    {
        var errors = new List<string>();
        var block = command.NewBlock;

        ValidateBlockHeight(command, errors);
        ValidatePreviousBlockHash(command, errors);
        ValidateBlockHashDeterminism(block, errors);
        ValidateEntryCount(block, errors);
        ValidateMerkleRoot(block, errors);
        ValidateEntryOrdering(block, errors);

        var accepted = errors.Count == 0;
        var chainContinuityValid = !errors.Exists(e =>
            e.Contains("BlockHeight") || e.Contains("PreviousBlockHash"));

        return new ChainAppendResult(
            BlockAccepted: accepted,
            NewChainHeight: accepted ? block.BlockHeight : command.CurrentChainHeight,
            AppendedBlockHash: accepted ? block.BlockHash : string.Empty,
            ChainContinuityValid: chainContinuityValid,
            GeneratedAt: DateTime.UtcNow,
            TraceId: command.TraceId,
            ValidationErrors: errors);
    }

    private static void ValidateBlockHeight(ChainAppendCommand command, List<string> errors)
    {
        var expectedHeight = command.CurrentChainHeight + 1;
        if (command.NewBlock.BlockHeight != expectedHeight)
            errors.Add($"BlockHeight mismatch: expected {expectedHeight}, got {command.NewBlock.BlockHeight}.");
    }

    private static void ValidatePreviousBlockHash(ChainAppendCommand command, List<string> errors)
    {
        if (command.CurrentChainHeight < 0)
        {
            if (command.NewBlock.PreviousBlockHash is not null)
                errors.Add("Genesis block must have null PreviousBlockHash.");
            return;
        }

        if (command.NewBlock.PreviousBlockHash != command.LatestBlockHash)
            errors.Add($"PreviousBlockHash mismatch: expected '{command.LatestBlockHash}', got '{command.NewBlock.PreviousBlockHash}'.");
    }

    private static void ValidateBlockHashDeterminism(LedgerChainBlock block, List<string> errors)
    {
        var expectedHash = ChainHashUtility.ComputeBlockHash(
            block.BlockHeight,
            block.PreviousBlockHash,
            block.MerkleRoot,
            block.EntryCount,
            block.CreatedAt);

        if (block.BlockHash != expectedHash)
            errors.Add("BlockHash is not deterministic — does not match recomputed hash.");
    }

    private static void ValidateEntryCount(LedgerChainBlock block, List<string> errors)
    {
        if (block.Entries.Count == 0)
            errors.Add("Block must contain at least one entry.");

        if (block.EntryCount != block.Entries.Count)
            errors.Add($"EntryCount ({block.EntryCount}) does not match actual entry count ({block.Entries.Count}).");
    }

    private static void ValidateMerkleRoot(LedgerChainBlock block, List<string> errors)
    {
        if (block.Entries.Count == 0)
            return;

        var entryHashes = block.Entries.Select(e => e.PayloadHash).ToList();
        var expectedRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);

        if (block.MerkleRoot != expectedRoot)
            errors.Add("MerkleRoot does not match computed value from entry hashes.");
    }

    private static void ValidateEntryOrdering(LedgerChainBlock block, List<string> errors)
    {
        for (var i = 1; i < block.Entries.Count; i++)
        {
            if (block.Entries[i].Timestamp < block.Entries[i - 1].Timestamp)
            {
                errors.Add($"Entries are not ordered: entry at index {i} has earlier timestamp than entry at index {i - 1}.");
                break;
            }
        }
    }
}
