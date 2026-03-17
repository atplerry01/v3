namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Engines.T2E.Identity.Models;
using Whycespace.Contracts.Engines;

public sealed class IdentityVerificationEngineTests
{
    private readonly IdentityVerificationEngine _engine = new();

    private EngineContext CreateContext(string operation, Dictionary<string, object> data)
    {
        data["operation"] = operation;
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "VerificationMutation",
            "partition-1", data);
    }

    [Fact]
    public async Task InitiateVerification_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var requestedBy = Guid.NewGuid().ToString();
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["verificationType"] = "Email",
            ["requestedBy"] = requestedBy,
            ["currentStatus"] = "Unverified"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityVerificationInitiated", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("Email", result.Output["verificationType"]);
        Assert.Equal("Pending", result.Output["status"]);
    }

    [Fact]
    public async Task CompleteVerification_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var verifiedBy = Guid.NewGuid().ToString();
        var context = CreateContext("complete", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["verificationType"] = "Phone",
            ["verifiedBy"] = verifiedBy,
            ["currentStatus"] = "Pending"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityVerificationCompleted", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("Phone", result.Output["verificationType"]);
        Assert.Equal("Verified", result.Output["status"]);
    }

    [Fact]
    public async Task RejectVerification_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var rejectedBy = Guid.NewGuid().ToString();
        var context = CreateContext("reject", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["verificationType"] = "Document",
            ["rejectedBy"] = rejectedBy,
            ["reason"] = "Document expired",
            ["currentStatus"] = "Pending"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityVerificationRejected", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("Document", result.Output["verificationType"]);
        Assert.Equal("Rejected", result.Output["status"]);
    }

    [Fact]
    public async Task InitiateVerification_FromRejected_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var requestedBy = Guid.NewGuid().ToString();
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["verificationType"] = "Biometric",
            ["requestedBy"] = requestedBy,
            ["currentStatus"] = "Rejected"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Pending", result.Output["status"]);
    }

    [Fact]
    public async Task InitiateVerification_FromVerified_ShouldFail()
    {
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "Email",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["currentStatus"] = "Verified"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitiateVerification_FromPending_ShouldFail()
    {
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "Email",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["currentStatus"] = "Pending"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CompleteVerification_FromUnverified_ShouldFail()
    {
        var context = CreateContext("complete", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "Email",
            ["verifiedBy"] = Guid.NewGuid().ToString(),
            ["currentStatus"] = "Unverified"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RejectVerification_FromUnverified_ShouldFail()
    {
        var context = CreateContext("reject", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "Email",
            ["rejectedBy"] = Guid.NewGuid().ToString(),
            ["reason"] = "Invalid document",
            ["currentStatus"] = "Unverified"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitiateVerification_MissingIdentityId_ShouldFail()
    {
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["verificationType"] = "Email",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitiateVerification_InvalidIdentityId_ShouldFail()
    {
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = Guid.Empty.ToString(),
            ["verificationType"] = "Email",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitiateVerification_InvalidVerificationType_ShouldFail()
    {
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "InvalidType",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RejectVerification_MissingReason_ShouldFail()
    {
        var context = CreateContext("reject", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verificationType"] = "Email",
            ["rejectedBy"] = Guid.NewGuid().ToString(),
            ["currentStatus"] = "Pending"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var context = CreateContext("unknown", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingOperation_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "VerificationMutation",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InitiateVerification_EventPayload_ShouldContainCorrectFields()
    {
        var identityId = Guid.NewGuid().ToString();
        var requestedBy = Guid.NewGuid().ToString();
        var context = CreateContext("initiate", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["verificationType"] = "Biometric",
            ["requestedBy"] = requestedBy,
            ["currentStatus"] = "Unverified"
        });

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal(identityId, eventPayload["identityId"]);
        Assert.Equal("Biometric", eventPayload["verificationType"]);
        Assert.Equal(requestedBy, eventPayload["requestedBy"]);
        Assert.Equal(1, eventPayload["eventVersion"]);
        Assert.Equal("whyce.identity.events", eventPayload["topic"]);
    }

    [Fact]
    public async Task ConcurrentVerifications_ShouldAllSucceed()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var verificationTypes = new[] { "Email", "Phone", "Document", "Biometric" };
                var context = CreateContext("initiate", new Dictionary<string, object>
                {
                    ["identityId"] = Guid.NewGuid().ToString(),
                    ["verificationType"] = verificationTypes[index % 4],
                    ["requestedBy"] = Guid.NewGuid().ToString(),
                    ["currentStatus"] = "Unverified"
                });
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }
}
