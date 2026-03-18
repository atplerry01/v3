namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Contracts.Engines;

public sealed class FederationEngineTests
{
    private readonly FederationEngine _engine = new();

    // --- Context factory helpers ---

    private static EngineContext CreateRegisterProviderContext(
        string? providerName = null,
        string? providerType = null,
        string? providerDomain = null,
        string? createdBy = null,
        object? providerExists = null)
    {
        var data = new Dictionary<string, object> { ["operation"] = "registerProvider" };

        if (providerName is not null) data["providerName"] = providerName;
        if (providerType is not null) data["providerType"] = providerType;
        if (providerDomain is not null) data["providerDomain"] = providerDomain;
        if (createdBy is not null) data["createdBy"] = createdBy;
        if (providerExists is not null) data["providerExists"] = providerExists;

        return new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "FederationMutation",
            "partition-1",
            data);
    }

    private static EngineContext CreateLinkContext(
        string? identityId = null,
        string? providerName = null,
        string? externalIdentityId = null,
        string? externalEmail = null,
        object? providerRegistered = null,
        object? identityExists = null,
        object? linkExists = null)
    {
        var data = new Dictionary<string, object> { ["operation"] = "link" };

        if (identityId is not null) data["identityId"] = identityId;
        if (providerName is not null) data["providerName"] = providerName;
        if (externalIdentityId is not null) data["externalIdentityId"] = externalIdentityId;
        if (externalEmail is not null) data["externalEmail"] = externalEmail;
        if (providerRegistered is not null) data["providerRegistered"] = providerRegistered;
        if (identityExists is not null) data["identityExists"] = identityExists;
        if (linkExists is not null) data["linkExists"] = linkExists;

        return new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "FederationMutation",
            "partition-1",
            data);
    }

    private static EngineContext CreateRevokeContext(
        string? identityId = null,
        string? providerName = null,
        string? reason = null,
        object? linkActive = null)
    {
        var data = new Dictionary<string, object> { ["operation"] = "revoke" };

        if (identityId is not null) data["identityId"] = identityId;
        if (providerName is not null) data["providerName"] = providerName;
        if (reason is not null) data["reason"] = reason;
        if (linkActive is not null) data["linkActive"] = linkActive;

        return new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "FederationMutation",
            "partition-1",
            data);
    }

    // --- Register federation provider tests ---

    [Fact]
    public async Task RegisterProvider_ShouldSucceed_WithValidInput()
    {
        var context = CreateRegisterProviderContext("GoogleOAuth", "OAuth", "accounts.google.com", "admin-1");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("FederationProviderRegistered", result.Events[0].EventType);
        Assert.Equal("GoogleOAuth", result.Output["providerName"]);
        Assert.Equal(true, result.Output["registered"]);
    }

    [Fact]
    public async Task RegisterProvider_ShouldEmitEvent_WithCorrectPayload()
    {
        var context = CreateRegisterProviderContext("AzureAD", "OpenID", "login.microsoftonline.com", "admin-1");

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("FederationProviderRegistered", evt.EventType);
        Assert.Equal("AzureAD", evt.Payload["providerName"]);
        Assert.Equal("OpenID", evt.Payload["providerType"]);
        Assert.Equal("login.microsoftonline.com", evt.Payload["providerDomain"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task RegisterProvider_MissingProviderName_ShouldFail()
    {
        var context = CreateRegisterProviderContext(providerType: "OAuth", providerDomain: "example.com", createdBy: "admin-1");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterProvider_InvalidProviderType_ShouldFail()
    {
        var context = CreateRegisterProviderContext("Test", "InvalidType", "example.com", "admin-1");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterProvider_DuplicateProvider_ShouldFail()
    {
        var context = CreateRegisterProviderContext("GoogleOAuth", "OAuth", "accounts.google.com", "admin-1", providerExists: true);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Link federated identity tests ---

    [Fact]
    public async Task LinkIdentity_ShouldSucceed_WithValidInput()
    {
        var identityId = Guid.NewGuid().ToString();
        var context = CreateLinkContext(identityId, "GoogleOAuth", "ext-123", "user@gmail.com");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("FederatedIdentityLinked", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("GoogleOAuth", result.Output["providerName"]);
        Assert.Equal(true, result.Output["linked"]);
    }

    [Fact]
    public async Task LinkIdentity_ShouldEmitEvent_WithCorrectPayload()
    {
        var identityId = Guid.NewGuid().ToString();
        var context = CreateLinkContext(identityId, "AzureAD", "ext-456", "user@company.com");

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("FederatedIdentityLinked", evt.EventType);
        Assert.Equal(identityId, evt.Payload["identityId"]);
        Assert.Equal("AzureAD", evt.Payload["providerName"]);
        Assert.Equal("ext-456", evt.Payload["externalIdentityId"]);
        Assert.Equal("user@company.com", evt.Payload["externalEmail"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task LinkIdentity_MissingIdentityId_ShouldFail()
    {
        var context = CreateLinkContext(providerName: "GoogleOAuth", externalIdentityId: "ext-123", externalEmail: "user@gmail.com");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LinkIdentity_ProviderNotRegistered_ShouldFail()
    {
        var context = CreateLinkContext(
            Guid.NewGuid().ToString(), "UnknownProvider", "ext-123", "user@test.com",
            providerRegistered: false);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LinkIdentity_IdentityDoesNotExist_ShouldFail()
    {
        var context = CreateLinkContext(
            Guid.NewGuid().ToString(), "GoogleOAuth", "ext-123", "user@test.com",
            identityExists: false);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LinkIdentity_DuplicateLink_ShouldFail()
    {
        var context = CreateLinkContext(
            Guid.NewGuid().ToString(), "GoogleOAuth", "ext-123", "user@test.com",
            linkExists: true);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Revoke federation link tests ---

    [Fact]
    public async Task RevokeLink_ShouldSucceed_WithValidInput()
    {
        var identityId = Guid.NewGuid().ToString();
        var context = CreateRevokeContext(identityId, "GoogleOAuth", "User requested removal");

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("FederationLinkRevoked", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal(false, result.Output["active"]);
    }

    [Fact]
    public async Task RevokeLink_ShouldEmitEvent_WithCorrectPayload()
    {
        var identityId = Guid.NewGuid().ToString();
        var context = CreateRevokeContext(identityId, "AzureAD", "Security concern");

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("FederationLinkRevoked", evt.EventType);
        Assert.Equal(identityId, evt.Payload["identityId"]);
        Assert.Equal("AzureAD", evt.Payload["providerName"]);
        Assert.Equal("Security concern", evt.Payload["reason"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task RevokeLink_MissingIdentityId_ShouldFail()
    {
        var context = CreateRevokeContext(providerName: "GoogleOAuth", reason: "Test");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeLink_MissingReason_ShouldFail()
    {
        var context = CreateRevokeContext(Guid.NewGuid().ToString(), "GoogleOAuth");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeLink_NoActiveLink_ShouldFail()
    {
        var context = CreateRevokeContext(
            Guid.NewGuid().ToString(), "GoogleOAuth", "Test reason",
            linkActive: false);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Missing operation test ---

    [Fact]
    public async Task MissingOperation_ShouldFail()
    {
        var data = new Dictionary<string, object>();
        var context = new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "FederationMutation",
            "partition-1",
            data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var data = new Dictionary<string, object> { ["operation"] = "unknown" };
        var context = new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "FederationMutation",
            "partition-1",
            data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Concurrency test ---

    [Fact]
    public async Task ParallelFederationLinks_ShouldAllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
        {
            var context = CreateLinkContext(
                Guid.NewGuid().ToString(),
                $"Provider-{i}",
                $"ext-{i}",
                $"user{i}@test.com");
            return _engine.ExecuteAsync(context);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Single(r.Events));
        Assert.All(results, r => Assert.Equal("FederatedIdentityLinked", r.Events[0].EventType));
    }
}
