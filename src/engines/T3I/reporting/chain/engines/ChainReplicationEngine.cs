using Whycespace.Engines.T3I.Reporting.Chain.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Reporting.Chain.Engines;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed class ChainReplicationEngine : IIntelligenceEngine<ChainReplicationCommand, ChainReplicationResult>
{
    public string EngineName => "ChainReplication";

    public IntelligenceResult<ChainReplicationResult> Execute(IntelligenceContext<ChainReplicationCommand> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var command = context.Input;
        var result = ExecuteCore(command);
        return IntelligenceResult<ChainReplicationResult>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static ChainReplicationResult ExecuteCore(ChainReplicationCommand command)
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
