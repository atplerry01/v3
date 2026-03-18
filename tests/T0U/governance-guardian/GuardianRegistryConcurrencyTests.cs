using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Registry;
using Whycespace.Systems.Upstream.Governance.Stores;

namespace Whycespace.GovernanceGuardian.Tests;

public class GuardianRegistryConcurrencyTests
{
    private readonly GuardianRecordStore _store = new();
    private readonly GuardianRegistry _registry;

    public GuardianRegistryConcurrencyTests()
    {
        _registry = new GuardianRegistry(_store);
    }

    private GuardianRecord CreateRecord(string? identityId = null) =>
        new(
            Guid.NewGuid(),
            identityId ?? Guid.NewGuid().ToString(),
            "Concurrent Guardian",
            GuardianRole.Guardian,
            GuardianStatus.Active,
            new List<string> { "mobility" },
            DateTime.UtcNow,
            "system",
            DateTime.UtcNow,
            null,
            new Dictionary<string, string>());

    [Fact]
    public void ConcurrentRegistrations_AllSucceed()
    {
        var records = Enumerable.Range(0, 100)
            .Select(_ => CreateRecord())
            .ToList();

        Parallel.ForEach(records, record =>
        {
            _registry.RegisterGuardian(record);
        });

        var all = _registry.GetGuardians();
        Assert.Equal(100, all.Count);
    }

    [Fact]
    public void ConcurrentReads_WhileRegistering_DoNotThrow()
    {
        var records = Enumerable.Range(0, 50)
            .Select(_ => CreateRecord())
            .ToList();

        var cts = new CancellationTokenSource();

        var readerTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _ = _registry.GetGuardians();
                _ = _registry.GetGuardiansByRole(GuardianRole.Guardian);
                _ = _registry.GetGuardiansByDomain("mobility");
            }
        });

        Parallel.ForEach(records, record =>
        {
            _registry.RegisterGuardian(record);
        });

        cts.Cancel();
        readerTask.Wait(TimeSpan.FromSeconds(5));

        Assert.Equal(50, _registry.GetGuardians().Count);
    }
}
