namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Identity;
using Whycespace.Contracts.Engines;

public sealed class ServiceIdentityEngineTests
{
    private readonly ServiceIdentityEngine _engine = new();

    private EngineContext CreateContext(string operation, Dictionary<string, object> data)
    {
        data["operation"] = operation;
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ServiceIdentityMutation",
            "partition-1", data);
    }

    [Fact]
    public async Task RegisterServiceIdentity_ShouldSucceed()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "PaymentGateway",
            ["serviceType"] = "Microservice",
            ["cluster"] = "finance-cluster",
            ["createdBy"] = "admin",
            ["permissions"] = "read,write,execute"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ServiceIdentityRegistered", result.Events[0].EventType);
        Assert.Equal("PaymentGateway", result.Output["serviceName"]);
        Assert.Equal("Microservice", result.Output["serviceType"]);
        Assert.True((bool)result.Output["active"]);
        Assert.NotNull(result.Output["apiKey"]);
        Assert.NotNull(result.Output["serviceIdentityId"]);
    }

    [Fact]
    public async Task RevokeServiceIdentity_ShouldSucceed()
    {
        var serviceIdentityId = Guid.NewGuid().ToString();
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["serviceIdentityId"] = serviceIdentityId,
            ["reason"] = "Service decommissioned",
            ["identityActive"] = true
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ServiceIdentityRevoked", result.Events[0].EventType);
        Assert.Equal(serviceIdentityId, result.Output["serviceIdentityId"]);
        Assert.False((bool)result.Output["active"]);
        Assert.Equal("Service decommissioned", result.Output["reason"]);
    }

    [Fact]
    public async Task RegisterServiceIdentity_CredentialGeneration_ShouldProduceApiKey()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "NotificationService",
            ["serviceType"] = "Microservice",
            ["cluster"] = "comms-cluster",
            ["createdBy"] = "admin",
            ["permissions"] = "send,read"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var apiKey = result.Output["apiKey"] as string;
        Assert.NotNull(apiKey);
        Assert.StartsWith("wsk_", apiKey);
    }

    [Fact]
    public async Task RegisterServiceIdentity_DuplicateServiceName_ShouldFail()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "ExistingService",
            ["serviceType"] = "Microservice",
            ["cluster"] = "test-cluster",
            ["createdBy"] = "admin",
            ["serviceNameExists"] = true
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterServiceIdentity_MissingServiceName_ShouldFail()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceType"] = "Microservice",
            ["cluster"] = "test-cluster",
            ["createdBy"] = "admin"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterServiceIdentity_InvalidServiceType_ShouldFail()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "TestService",
            ["serviceType"] = "InvalidType",
            ["cluster"] = "test-cluster",
            ["createdBy"] = "admin"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterServiceIdentity_MissingCluster_ShouldFail()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "TestService",
            ["serviceType"] = "Microservice",
            ["createdBy"] = "admin"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterServiceIdentity_MissingCreatedBy_ShouldFail()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "TestService",
            ["serviceType"] = "Microservice",
            ["cluster"] = "test-cluster"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeServiceIdentity_MissingServiceIdentityId_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["reason"] = "No longer needed",
            ["identityActive"] = true
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeServiceIdentity_InvalidServiceIdentityId_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["serviceIdentityId"] = Guid.Empty.ToString(),
            ["reason"] = "No longer needed",
            ["identityActive"] = true
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeServiceIdentity_MissingReason_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["serviceIdentityId"] = Guid.NewGuid().ToString(),
            ["identityActive"] = true
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeServiceIdentity_InactiveIdentity_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["serviceIdentityId"] = Guid.NewGuid().ToString(),
            ["reason"] = "Test revocation",
            ["identityActive"] = false
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var context = CreateContext("unknown", new Dictionary<string, object>
        {
            ["serviceName"] = "TestService"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingOperation_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ServiceIdentityMutation",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterServiceIdentity_EventPayload_ShouldContainCorrectFields()
    {
        var context = CreateContext("register", new Dictionary<string, object>
        {
            ["serviceName"] = "AuditLogger",
            ["serviceType"] = "InfrastructureModule",
            ["cluster"] = "platform-cluster",
            ["createdBy"] = "system-admin",
            ["permissions"] = "log,read"
        });

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal("AuditLogger", eventPayload["serviceName"]);
        Assert.Equal("InfrastructureModule", eventPayload["serviceType"]);
        Assert.Equal("platform-cluster", eventPayload["cluster"]);
        Assert.Equal("system-admin", eventPayload["createdBy"]);
        Assert.Equal(1, eventPayload["eventVersion"]);
        Assert.Equal("whyce.identity.events", eventPayload["topic"]);
    }

    [Fact]
    public async Task RegisterServiceIdentity_AllServiceTypes_ShouldSucceed()
    {
        var serviceTypes = new[] { "Microservice", "InfrastructureModule", "AutomationAgent", "IntegrationConnector" };

        foreach (var serviceType in serviceTypes)
        {
            var context = CreateContext("register", new Dictionary<string, object>
            {
                ["serviceName"] = $"Test-{serviceType}",
                ["serviceType"] = serviceType,
                ["cluster"] = "test-cluster",
                ["createdBy"] = "admin"
            });

            var result = await _engine.ExecuteAsync(context);

            Assert.True(result.Success, $"Failed for service type: {serviceType}");
        }
    }

    [Fact]
    public async Task ConcurrentRegistrations_ShouldAllSucceed()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var serviceTypes = new[] { "Microservice", "InfrastructureModule", "AutomationAgent", "IntegrationConnector" };
                var context = CreateContext("register", new Dictionary<string, object>
                {
                    ["serviceName"] = $"ConcurrentService-{index}",
                    ["serviceType"] = serviceTypes[index % 4],
                    ["cluster"] = $"cluster-{index % 3}",
                    ["createdBy"] = "admin",
                    ["permissions"] = "read,write"
                });
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Single(r.Events));
    }
}
