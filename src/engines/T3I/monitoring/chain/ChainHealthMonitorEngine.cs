namespace Whycespace.Engines.T3I.Monitoring.Chain;

public sealed class ChainHealthMonitorEngine
{
    private const long ReplicationLagDegradedThreshold = 5;
    private const long AnchorLagDegradedThreshold = 10;

    public ChainHealthMonitorResult Execute(ChainHealthMonitorCommand command)
    {
        var currentChainHeight = DetermineChainHeight(command);
        var blockContinuityStatus = VerifyBlockContinuity(command.Blocks);
        var ledgerIntegrityStatus = VerifyLedgerIntegrity(command.LedgerEntries);
        var replicationLag = currentChainHeight - command.ReplicationHeight;
        var anchorLag = currentChainHeight - command.AnchoredBlockHeight;
        var snapshotValid = command.LatestSnapshotHeight <= currentChainHeight;

        var chainHealthStatus = ClassifyHealth(
            blockContinuityStatus,
            ledgerIntegrityStatus,
            replicationLag,
            anchorLag,
            snapshotValid);

        return new ChainHealthMonitorResult(
            currentChainHeight,
            command.LatestSnapshotHeight,
            replicationLag,
            anchorLag,
            ledgerIntegrityStatus,
            blockContinuityStatus,
            chainHealthStatus,
            command.Timestamp,
            command.TraceId);
    }

    private static long DetermineChainHeight(ChainHealthMonitorCommand command)
    {
        if (command.Blocks.Count == 0)
            return 0;

        long maxHeight = 0;
        for (var i = 0; i < command.Blocks.Count; i++)
        {
            if (command.Blocks[i].BlockNumber > maxHeight)
                maxHeight = command.Blocks[i].BlockNumber;
        }

        return maxHeight;
    }

    private static string VerifyBlockContinuity(IReadOnlyList<Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock> blocks)
    {
        if (blocks.Count <= 1)
            return "Valid";

        var sorted = new List<Whycespace.Systems.Upstream.WhyceChain.Models.ChainBlock>(blocks);
        sorted.Sort((a, b) => a.BlockNumber.CompareTo(b.BlockNumber));

        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].BlockNumber != sorted[i - 1].BlockNumber + 1)
                return "Broken";

            if (!string.Equals(sorted[i].PreviousBlockHash, sorted[i - 1].BlockHash, StringComparison.Ordinal))
                return "Broken";
        }

        return "Valid";
    }

    private static string VerifyLedgerIntegrity(IReadOnlyList<Whycespace.Systems.Upstream.WhyceChain.Models.ChainLedgerEntry> entries)
    {
        if (entries.Count <= 1)
            return "Valid";

        for (var i = 1; i < entries.Count; i++)
        {
            if (!string.Equals(entries[i].PreviousHash, entries[i - 1].PayloadHash, StringComparison.Ordinal))
                return "Broken";
        }

        return "Valid";
    }

    private static string ClassifyHealth(
        string blockContinuityStatus,
        string ledgerIntegrityStatus,
        long replicationLag,
        long anchorLag,
        bool snapshotValid)
    {
        if (blockContinuityStatus == "Broken" || ledgerIntegrityStatus == "Broken")
            return "Critical";

        if (replicationLag > ReplicationLagDegradedThreshold
            || anchorLag > AnchorLagDegradedThreshold
            || !snapshotValid)
            return "Degraded";

        return "Healthy";
    }
}
