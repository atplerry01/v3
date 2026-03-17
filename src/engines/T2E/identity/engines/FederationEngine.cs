namespace Whycespace.Engines.T2E.Identity.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Federation", EngineTier.T2E, EngineKind.Mutation, "LinkFederatedIdentityCommand", typeof(EngineEvent))]
public sealed class FederationEngine : IEngine
{
    private static readonly HashSet<string> ValidProviderTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OAuth", "OpenID", "SAML"
    };

    public string Name => "Federation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "registerProvider" => RegisterFederationProvider(context),
            "link" => LinkFederatedIdentity(context),
            "revoke" => RevokeFederationLink(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> RegisterFederationProvider(EngineContext context)
    {
        // 1. Validate command input
        var providerName = context.Data.GetValueOrDefault("providerName") as string;
        if (string.IsNullOrEmpty(providerName))
            return Task.FromResult(EngineResult.Fail("Missing providerName"));

        var providerType = context.Data.GetValueOrDefault("providerType") as string;
        if (string.IsNullOrEmpty(providerType) || !ValidProviderTypes.Contains(providerType))
            return Task.FromResult(EngineResult.Fail($"Invalid or missing providerType. Valid: {string.Join(", ", ValidProviderTypes)}"));

        var providerDomain = context.Data.GetValueOrDefault("providerDomain") as string;
        if (string.IsNullOrEmpty(providerDomain))
            return Task.FromResult(EngineResult.Fail("Missing providerDomain"));

        var createdBy = context.Data.GetValueOrDefault("createdBy") as string;
        if (string.IsNullOrEmpty(createdBy))
            return Task.FromResult(EngineResult.Fail("Missing createdBy"));

        // 2. Check for duplicate provider
        var providerExists = context.Data.GetValueOrDefault("providerExists");
        if (providerExists is true or "true")
            return Task.FromResult(EngineResult.Fail($"Federation provider '{providerName}' already registered"));

        // 3. Emit FederationProviderRegisteredEvent
        var registeredAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("FederationProviderRegistered", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["providerName"] = providerName,
                    ["providerType"] = providerType,
                    ["providerDomain"] = providerDomain,
                    ["createdBy"] = createdBy,
                    ["registeredAt"] = registeredAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 4. Return result
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["providerName"] = providerName,
                ["providerType"] = providerType,
                ["providerDomain"] = providerDomain,
                ["registered"] = true,
                ["registeredAt"] = registeredAt.ToString("O")
            }));
    }

    private static Task<EngineResult> LinkFederatedIdentity(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid))
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var providerName = context.Data.GetValueOrDefault("providerName") as string;
        if (string.IsNullOrEmpty(providerName))
            return Task.FromResult(EngineResult.Fail("Missing providerName"));

        var externalIdentityId = context.Data.GetValueOrDefault("externalIdentityId") as string;
        if (string.IsNullOrEmpty(externalIdentityId))
            return Task.FromResult(EngineResult.Fail("Missing externalIdentityId"));

        var externalEmail = context.Data.GetValueOrDefault("externalEmail") as string;
        if (string.IsNullOrEmpty(externalEmail))
            return Task.FromResult(EngineResult.Fail("Missing externalEmail"));

        // 2. Verify provider is registered
        var providerRegistered = context.Data.GetValueOrDefault("providerRegistered");
        if (providerRegistered is false or "false")
            return Task.FromResult(EngineResult.Fail($"Federation provider '{providerName}' is not registered"));

        // 3. Verify identity exists internally
        var identityExists = context.Data.GetValueOrDefault("identityExists");
        if (identityExists is false or "false")
            return Task.FromResult(EngineResult.Fail($"Identity '{identityId}' does not exist"));

        // 4. Check for duplicate federation link
        var linkExists = context.Data.GetValueOrDefault("linkExists");
        if (linkExists is true or "true")
            return Task.FromResult(EngineResult.Fail($"Federation link already exists for identity '{identityId}' with provider '{providerName}'"));

        // 5. Emit FederatedIdentityLinkedEvent
        var linkedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("FederatedIdentityLinked", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["providerName"] = providerName,
                    ["externalIdentityId"] = externalIdentityId,
                    ["externalEmail"] = externalEmail,
                    ["linkedAt"] = linkedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 6. Return FederationLinkResult
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["providerName"] = providerName,
                ["externalIdentityId"] = externalIdentityId,
                ["linked"] = true,
                ["linkedAt"] = linkedAt.ToString("O")
            }));
    }

    private static Task<EngineResult> RevokeFederationLink(EngineContext context)
    {
        // 1. Validate command input
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var identityGuid))
            return Task.FromResult(EngineResult.Fail("Missing or invalid identityId"));

        var providerName = context.Data.GetValueOrDefault("providerName") as string;
        if (string.IsNullOrEmpty(providerName))
            return Task.FromResult(EngineResult.Fail("Missing providerName"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason"));

        // 2. Verify link exists and is active
        var linkActive = context.Data.GetValueOrDefault("linkActive");
        if (linkActive is false or "false")
            return Task.FromResult(EngineResult.Fail($"No active federation link for identity '{identityId}' with provider '{providerName}'"));

        // 3. Emit FederationLinkRevokedEvent
        var revokedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("FederationLinkRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["identityId"] = identityId,
                    ["providerName"] = providerName,
                    ["reason"] = reason,
                    ["revokedAt"] = revokedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 4. Return result
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["identityId"] = identityId,
                ["providerName"] = providerName,
                ["active"] = false,
                ["reason"] = reason,
                ["revokedAt"] = revokedAt.ToString("O")
            }));
    }
}
