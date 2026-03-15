namespace Whycespace.Engines.T3I.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class ChainReplicationEngine
{
    public ChainReplicationResult Execute(ChainReplicationCommand command)
    {
        var maxHeight = command.SourceBlocks.Count == 0
            ? -1L
            : command.SourceBlocks.Max(b => b.BlockNumber);

        if (command.TargetHeight < 0 || command.TargetHeight > maxHeight)
            throw new ArgumentException(
                $"Target height {command.TargetHeight} is invalid. Source chain max height is {maxHeight}.");

        var replicationBlocks = command.SourceBlocks
            .Where(b => b.BlockNumber <= command.TargetHeight)
            .OrderBy(b => b.BlockNumber)
            .ToList();

        var replicationBlockIds = new HashSet<string>(
            replicationBlocks.SelectMany(b => b.EntryIds),
            StringComparer.Ordinal);

        var replicationEntries = command.SourceLedgerEntries
            .Where(e => replicationBlockIds.Contains(e.EntryId))
            .ToList();

        var replicationEntryCount = replicationEntries.Count;

        var latestBlockHash = replicationBlocks.Count > 0
            ? replicationBlocks[^1].BlockHash
            : "";

        var replicationHash = ComputeReplicationHash(
            command.TargetHeight, replicationEntryCount, latestBlockHash);

        return new ChainReplicationResult(
            replicationBlocks,
            replicationEntries,
            command.TargetHeight,
            replicationHash,
            replicationEntryCount,
            command.Timestamp,
            command.TraceId);
    }

    private static string ComputeReplicationHash(
        long replicatedHeight, int replicationEntryCount, string latestBlockHash)
    {
        var input = $"{replicatedHeight}:{replicationEntryCount}:{latestBlockHash}";
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
