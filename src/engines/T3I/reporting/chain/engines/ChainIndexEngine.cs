using Whycespace.Engines.T3I.Reporting.Chain.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Reporting.Chain.Engines;

public sealed class ChainIndexEngine : IIntelligenceEngine<ChainIndexCommand, ChainIndexResult>
{
    public string EngineName => "ChainIndex";

    public IntelligenceResult<ChainIndexResult> Execute(IntelligenceContext<ChainIndexCommand> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var command = context.Input;
        var result = ExecuteCore(command);
        return IntelligenceResult<ChainIndexResult>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static ChainIndexResult ExecuteCore(ChainIndexCommand command)
    {
        var entryBySequence = new Dictionary<long, string>();
        var entryByHash = new Dictionary<string, long>();
        var traceIndex = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var correlationIndex = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        for (var i = 0; i < command.LedgerEntries.Count; i++)
        {
            var entry = command.LedgerEntries[i];
            long sequence = i;

            entryBySequence[sequence] = entry.PayloadHash;

            if (!entryByHash.ContainsKey(entry.PayloadHash))
                entryByHash[entry.PayloadHash] = sequence;

            if (!string.IsNullOrEmpty(command.TraceId))
            {
                if (!traceIndex.TryGetValue(command.TraceId, out var traceList))
                {
                    traceList = [];
                    traceIndex[command.TraceId] = traceList;
                }
                traceList.Add(entry.PayloadHash);
            }

            if (!string.IsNullOrEmpty(command.CorrelationId))
            {
                if (!correlationIndex.TryGetValue(command.CorrelationId, out var corrList))
                {
                    corrList = [];
                    correlationIndex[command.CorrelationId] = corrList;
                }
                corrList.Add(entry.PayloadHash);
            }
        }

        var blockByHeight = new Dictionary<long, string>();
        var blockByHash = new Dictionary<string, long>();

        foreach (var block in command.Blocks)
        {
            blockByHeight[block.BlockNumber] = block.BlockHash;

            if (!blockByHash.ContainsKey(block.BlockHash))
                blockByHash[block.BlockHash] = block.BlockNumber;
        }

        return new ChainIndexResult(
            entryBySequence,
            entryByHash,
            blockByHeight,
            blockByHash,
            traceIndex,
            correlationIndex,
            command.Timestamp,
            command.TraceId);
    }
}
