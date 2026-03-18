namespace Whycespace.Runtime.Persistence.Diagnostics;

using System.Diagnostics;

public static class PersistenceMetrics
{
    private static readonly ActivitySource ActivitySource = new("Whycespace.Persistence");

    private static long _totalReads;
    private static long _totalWrites;
    private static long _totalErrors;

    public static Activity? StartRead(string storeName) =>
        ActivitySource.StartActivity($"persistence.read.{storeName}");

    public static Activity? StartWrite(string storeName) =>
        ActivitySource.StartActivity($"persistence.write.{storeName}");

    public static void RecordRead() => Interlocked.Increment(ref _totalReads);
    public static void RecordWrite() => Interlocked.Increment(ref _totalWrites);
    public static void RecordError() => Interlocked.Increment(ref _totalErrors);

    public static long TotalReads => Interlocked.Read(ref _totalReads);
    public static long TotalWrites => Interlocked.Read(ref _totalWrites);
    public static long TotalErrors => Interlocked.Read(ref _totalErrors);
}
