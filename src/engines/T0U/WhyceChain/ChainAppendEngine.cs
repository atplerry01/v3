namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

public sealed class ChainAppendEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly BlockBuilderEngine _builderEngine;
    private readonly IntegrityVerificationEngine _integrityEngine;

    public ChainAppendEngine(
        ChainBlockStore blockStore,
        BlockBuilderEngine builderEngine,
        IntegrityVerificationEngine integrityEngine)
    {
        _blockStore = blockStore;
        _builderEngine = builderEngine;
        _integrityEngine = integrityEngine;
    }

    public ChainBlock? AppendBlock()
    {
        var block = _builderEngine.BuildBlock();
        if (block is null)
            return null;

        if (!ValidateAppend(block))
            throw new InvalidOperationException("Block validation failed after append");

        return block;
    }

    public bool ValidateAppend(ChainBlock block)
    {
        if (!_integrityEngine.VerifyBlock(block.BlockNumber))
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
}
