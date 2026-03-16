using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRegistryStoreTests
{
    private static IdentityRecord CreateRecord(
        Guid? id = null,
        IdentityType type = IdentityType.User,
        string? email = null,
        string? phone = null)
    {
        return new IdentityRecord(
            id ?? Guid.NewGuid(),
            type,
            IdentityStatus.Pending,
            DateTime.UtcNow,
            DateTime.UtcNow,
            primaryEmail: email,
            primaryPhone: phone);
    }

    [Fact]
    public void RegisterIdentity_ShouldStoreRecord()
    {
        var store = new IdentityRegistryStore();
        var record = CreateRecord();

        store.RegisterIdentity(record);

        var result = store.GetIdentity(record.IdentityId);
        Assert.Equal(record.IdentityId, result.IdentityId);
        Assert.Equal(IdentityType.User, result.Type);
        Assert.Equal(IdentityStatus.Pending, result.Status);
    }

    [Fact]
    public void GetIdentity_ById_ShouldReturnRecord()
    {
        var store = new IdentityRegistryStore();
        var id = Guid.NewGuid();
        var record = CreateRecord(id: id, type: IdentityType.Operator);

        store.RegisterIdentity(record);

        var result = store.GetIdentity(id);
        Assert.Equal(id, result.IdentityId);
        Assert.Equal(IdentityType.Operator, result.Type);
    }

    [Fact]
    public void GetIdentity_NotFound_ShouldThrow()
    {
        var store = new IdentityRegistryStore();

        Assert.Throws<KeyNotFoundException>(() => store.GetIdentity(Guid.NewGuid()));
    }

    [Fact]
    public void GetIdentityByEmail_ShouldReturnMatchingRecord()
    {
        var store = new IdentityRegistryStore();
        var record = CreateRecord(email: "alice@example.com");

        store.RegisterIdentity(record);

        var result = store.GetIdentityByEmail("alice@example.com");
        Assert.NotNull(result);
        Assert.Equal(record.IdentityId, result.IdentityId);
    }

    [Fact]
    public void GetIdentityByEmail_CaseInsensitive_ShouldReturnRecord()
    {
        var store = new IdentityRegistryStore();
        var record = CreateRecord(email: "Alice@Example.com");

        store.RegisterIdentity(record);

        var result = store.GetIdentityByEmail("alice@example.com");
        Assert.NotNull(result);
        Assert.Equal(record.IdentityId, result.IdentityId);
    }

    [Fact]
    public void GetIdentityByEmail_NotFound_ShouldReturnNull()
    {
        var store = new IdentityRegistryStore();

        var result = store.GetIdentityByEmail("nobody@example.com");
        Assert.Null(result);
    }

    [Fact]
    public void GetIdentityByPhone_ShouldReturnMatchingRecord()
    {
        var store = new IdentityRegistryStore();
        var record = CreateRecord(phone: "+1234567890");

        store.RegisterIdentity(record);

        var result = store.GetIdentityByPhone("+1234567890");
        Assert.NotNull(result);
        Assert.Equal(record.IdentityId, result.IdentityId);
    }

    [Fact]
    public void GetIdentityByPhone_NotFound_ShouldReturnNull()
    {
        var store = new IdentityRegistryStore();

        var result = store.GetIdentityByPhone("+0000000000");
        Assert.Null(result);
    }

    [Fact]
    public void UpdateIdentityStatus_ShouldChangeStatus()
    {
        var store = new IdentityRegistryStore();
        var record = CreateRecord();

        store.RegisterIdentity(record);
        store.UpdateIdentityStatus(record.IdentityId, IdentityStatus.Verified);

        var result = store.GetIdentity(record.IdentityId);
        Assert.Equal(IdentityStatus.Verified, result.Status);
    }

    [Fact]
    public void UpdateIdentityStatus_NotFound_ShouldThrow()
    {
        var store = new IdentityRegistryStore();

        Assert.Throws<KeyNotFoundException>(
            () => store.UpdateIdentityStatus(Guid.NewGuid(), IdentityStatus.Suspended));
    }

    [Fact]
    public void ListIdentities_ShouldReturnPagedResults()
    {
        var store = new IdentityRegistryStore();

        for (int i = 0; i < 25; i++)
        {
            store.RegisterIdentity(CreateRecord());
        }

        var page1 = store.ListIdentities(1, 10);
        var page2 = store.ListIdentities(2, 10);
        var page3 = store.ListIdentities(3, 10);

        Assert.Equal(10, page1.Count);
        Assert.Equal(10, page2.Count);
        Assert.Equal(5, page3.Count);
    }

    [Fact]
    public void ListIdentities_InvalidPage_ShouldThrow()
    {
        var store = new IdentityRegistryStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.ListIdentities(0, 10));
    }

    [Fact]
    public void ListIdentities_InvalidPageSize_ShouldThrow()
    {
        var store = new IdentityRegistryStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.ListIdentities(1, 0));
    }

    [Fact]
    public void RegisterIdentity_Duplicate_ShouldThrow()
    {
        var store = new IdentityRegistryStore();
        var id = Guid.NewGuid();
        var record = CreateRecord(id: id);

        store.RegisterIdentity(record);

        Assert.Throws<InvalidOperationException>(
            () => store.RegisterIdentity(CreateRecord(id: id)));
    }

    [Fact]
    public void RegisterIdentity_Null_ShouldThrow()
    {
        var store = new IdentityRegistryStore();

        Assert.Throws<ArgumentNullException>(() => store.RegisterIdentity(null!));
    }

    [Fact]
    public async Task ConcurrentRegistration_100Identities_ShouldBeSafe()
    {
        var store = new IdentityRegistryStore();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                store.RegisterIdentity(CreateRecord());
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, store.Count);
    }

    [Fact]
    public async Task ConcurrentReads_ShouldBeSafe()
    {
        var store = new IdentityRegistryStore();
        var records = Enumerable.Range(0, 50)
            .Select(_ => CreateRecord(email: $"{Guid.NewGuid()}@test.com"))
            .ToList();

        foreach (var record in records)
        {
            store.RegisterIdentity(record);
        }

        var readByIdTasks = records.Select(r =>
            Task.Run(() => store.GetIdentity(r.IdentityId))).ToArray();
        var readByEmailTasks = records.Select(r =>
            Task.Run(() => store.GetIdentityByEmail(r.PrimaryEmail!))).ToArray();

        var byIdResults = await Task.WhenAll(readByIdTasks);
        var byEmailResults = await Task.WhenAll(readByEmailTasks);

        Assert.All(byIdResults, r => Assert.NotNull(r));
        Assert.All(byEmailResults, r => Assert.NotNull(r));
    }
}
