using Whycespace.ReliabilityRuntime.Idempotency;
using System.Collections.Concurrent;

namespace Whycespace.RuntimeConcurrencyTests;

public sealed class IdempotencyStressTests
{
    [Fact]
    public void ConcurrentAllowExecution_SameId_OnlyOneAllowed()
    {
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        var allowedCount = 0;

        Parallel.For(0, 100, _ =>
        {
            if (guard.AllowExecution("TEST-ID"))
                Interlocked.Increment(ref allowedCount);
        });

        Assert.Equal(1, allowedCount);
    }

    [Fact]
    public void SerializedAllowExecution_1000UniqueIds_AllAllowed()
    {
        // Idempotency components are designed for single-threaded access
        // within partition workers. This test validates correctness under
        // serialized execution with high volume.
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        var allowedCount = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (guard.AllowExecution($"EXEC-{i}"))
                allowedCount++;
        }

        Assert.Equal(1000, allowedCount);
    }

    [Fact]
    public void ConcurrentRegister_SameId_OnlyOneSucceeds()
    {
        var registry = new DuplicateExecutionRegistry();

        var successCount = 0;

        Parallel.For(0, 100, _ =>
        {
            if (registry.Register("DUPLICATE-ID"))
                Interlocked.Increment(ref successCount);
        });

        Assert.Equal(1, successCount);
        Assert.True(registry.Exists("DUPLICATE-ID"));
    }

    [Fact]
    public void SerializedRegister_1000UniqueIds_AllRegistered()
    {
        // Validates high-volume serialized registration matches the
        // partition-worker execution model.
        var registry = new DuplicateExecutionRegistry();

        var successCount = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (registry.Register($"EXEC-{i}"))
                successCount++;
        }

        Assert.Equal(1000, successCount);

        for (var i = 0; i < 1000; i++)
        {
            Assert.True(registry.Exists($"EXEC-{i}"));
        }
    }

    [Fact]
    public void ConcurrentDuplicateAttempts_MultipleIds_ExactlyOnePerGroup()
    {
        // Simulate multiple concurrent attempts for each of several IDs.
        // Each ID should only be allowed once.
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        var allowedIds = new ConcurrentBag<string>();

        // 10 unique IDs, each attempted 50 times concurrently
        Parallel.For(0, 500, i =>
        {
            var id = $"GROUP-{i % 10}";
            if (guard.AllowExecution(id))
                allowedIds.Add(id);
        });

        Assert.Equal(10, allowedIds.Count);
        Assert.Equal(10, allowedIds.Distinct().Count());
    }

    [Fact]
    public void SerializedMixedOperations_RegisterAndExists_Consistent()
    {
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        // Pre-register some IDs
        for (var i = 0; i < 50; i++)
        {
            guard.AllowExecution($"PRE-{i}");
        }

        var newAllowed = 0;
        var duplicateBlocked = 0;

        for (var i = 0; i < 100; i++)
        {
            var id = i < 50 ? $"PRE-{i}" : $"NEW-{i}";

            if (guard.AllowExecution(id))
                newAllowed++;
            else
                duplicateBlocked++;
        }

        // PRE-0..PRE-49 should all be blocked (already registered)
        Assert.Equal(50, duplicateBlocked);

        // NEW-50..NEW-99 should all be allowed
        Assert.Equal(50, newAllowed);
    }
}
