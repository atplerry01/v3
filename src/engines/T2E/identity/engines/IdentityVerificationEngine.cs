namespace Whycespace.Engines.T2E.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityVerificationEngine", EngineTier.T2E, EngineKind.Mutation, "IdentityVerificationMutationRequest", typeof(EngineEvent))]
public sealed class IdentityVerificationEngine : IEngine
{
    private static readonly HashSet<string> ValidVerificationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email", "Phone", "Document", "Biometric"
    };

    public string Name => "IdentityVerificationEngine";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "initiate" => InitiateVerification(context),
            "complete" => CompleteVerification(context),
            "reject" => RejectVerification(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> InitiateVerification(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var verificationType = context.Data.GetValueOrDefault("verificationType") as string;
        if (string.IsNullOrEmpty(verificationType))
            return Task.FromResult(EngineResult.Fail("Missing verificationType"));

        if (!ValidVerificationTypes.Contains(verificationType))
            return Task.FromResult(EngineResult.Fail($"Invalid verificationType: {verificationType}. Expected: Email, Phone, Document, or Biometric"));

        var requestedBy = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedBy))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));

        if (!Guid.TryParse(requestedBy, out _))
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy"));

        var currentStatus = context.Data.GetValueOrDefault("currentStatus") as string ?? "Unverified";
        if (currentStatus != "Unverified" && currentStatus != "Rejected")
            return Task.FromResult(EngineResult.Fail($"Invalid state transition: cannot initiate verification from status '{currentStatus}'. Allowed: Unverified, Rejected"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityVerificationInitiated", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["verificationType"] = verificationType,
                    ["requestedBy"] = requestedBy,
                    ["requestedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["verificationType"] = verificationType,
                ["status"] = "Pending",
                ["executedBy"] = requestedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }

    private Task<EngineResult> CompleteVerification(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var verificationType = context.Data.GetValueOrDefault("verificationType") as string;
        if (string.IsNullOrEmpty(verificationType))
            return Task.FromResult(EngineResult.Fail("Missing verificationType"));

        if (!ValidVerificationTypes.Contains(verificationType))
            return Task.FromResult(EngineResult.Fail($"Invalid verificationType: {verificationType}. Expected: Email, Phone, Document, or Biometric"));

        var verifiedBy = context.Data.GetValueOrDefault("verifiedBy") as string;
        if (string.IsNullOrEmpty(verifiedBy))
            return Task.FromResult(EngineResult.Fail("Missing verifiedBy"));

        if (!Guid.TryParse(verifiedBy, out _))
            return Task.FromResult(EngineResult.Fail("Invalid verifiedBy"));

        var currentStatus = context.Data.GetValueOrDefault("currentStatus") as string ?? "";
        if (currentStatus != "Pending")
            return Task.FromResult(EngineResult.Fail($"Invalid state transition: cannot complete verification from status '{currentStatus}'. Allowed: Pending"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityVerificationCompleted", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["verificationType"] = verificationType,
                    ["verifiedBy"] = verifiedBy,
                    ["verifiedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["verificationType"] = verificationType,
                ["status"] = "Verified",
                ["executedBy"] = verifiedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }

    private Task<EngineResult> RejectVerification(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId))
            return Task.FromResult(EngineResult.Fail("Missing identityId"));

        if (!Guid.TryParse(identityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid identityId"));

        var verificationType = context.Data.GetValueOrDefault("verificationType") as string;
        if (string.IsNullOrEmpty(verificationType))
            return Task.FromResult(EngineResult.Fail("Missing verificationType"));

        if (!ValidVerificationTypes.Contains(verificationType))
            return Task.FromResult(EngineResult.Fail($"Invalid verificationType: {verificationType}. Expected: Email, Phone, Document, or Biometric"));

        var rejectedBy = context.Data.GetValueOrDefault("rejectedBy") as string;
        if (string.IsNullOrEmpty(rejectedBy))
            return Task.FromResult(EngineResult.Fail("Missing rejectedBy"));

        if (!Guid.TryParse(rejectedBy, out _))
            return Task.FromResult(EngineResult.Fail("Invalid rejectedBy"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason"));

        var currentStatus = context.Data.GetValueOrDefault("currentStatus") as string ?? "";
        if (currentStatus != "Pending")
            return Task.FromResult(EngineResult.Fail($"Invalid state transition: cannot reject verification from status '{currentStatus}'. Allowed: Pending"));

        var timestamp = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("IdentityVerificationRejected", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["verificationType"] = verificationType,
                    ["rejectedBy"] = rejectedBy,
                    ["reason"] = reason,
                    ["rejectedAt"] = timestamp.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["verificationType"] = verificationType,
                ["status"] = "Rejected",
                ["executedBy"] = rejectedBy,
                ["executedAt"] = timestamp.ToString("O")
            }));
    }
}
