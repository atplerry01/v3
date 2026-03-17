namespace Whycespace.Engines.T2E.System.Identity;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("ServiceIdentity", EngineTier.T2E, EngineKind.Mutation, "RegisterServiceIdentityCommand", typeof(EngineEvent))]
public sealed class ServiceIdentityEngine : IEngine
{
    private static readonly HashSet<string> ValidServiceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microservice", "InfrastructureModule", "AutomationAgent", "IntegrationConnector"
    };

    public string Name => "ServiceIdentity";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        return operation switch
        {
            "register" => RegisterServiceIdentity(context),
            "revoke" => RevokeServiceIdentity(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> RegisterServiceIdentity(EngineContext context)
    {
        // 1. Validate command input
        var serviceName = context.Data.GetValueOrDefault("serviceName") as string;
        if (string.IsNullOrEmpty(serviceName))
            return Task.FromResult(EngineResult.Fail("Missing serviceName"));

        var serviceType = context.Data.GetValueOrDefault("serviceType") as string;
        if (string.IsNullOrEmpty(serviceType) || !ValidServiceTypes.Contains(serviceType))
            return Task.FromResult(EngineResult.Fail($"Invalid or missing serviceType. Valid: {string.Join(", ", ValidServiceTypes)}"));

        var cluster = context.Data.GetValueOrDefault("cluster") as string;
        if (string.IsNullOrEmpty(cluster))
            return Task.FromResult(EngineResult.Fail("Missing cluster"));

        var createdBy = context.Data.GetValueOrDefault("createdBy") as string;
        if (string.IsNullOrEmpty(createdBy))
            return Task.FromResult(EngineResult.Fail("Missing createdBy"));

        // 2. Check for duplicate service name
        var serviceNameExists = context.Data.GetValueOrDefault("serviceNameExists");
        if (serviceNameExists is true or "true")
            return Task.FromResult(EngineResult.Fail($"Service identity with name '{serviceName}' already exists"));

        // 3. Resolve permissions
        var permissions = ResolvePermissions(context.Data.GetValueOrDefault("permissions"));

        // 4. Generate service identity id
        var serviceIdentityId = Guid.NewGuid();

        // 5. Generate service API key
        var apiKey = GenerateApiKey(serviceIdentityId, serviceName);

        // 6. Emit ServiceIdentityRegisteredEvent
        var createdAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ServiceIdentityRegistered", serviceIdentityId,
                new Dictionary<string, object>
                {
                    ["serviceIdentityId"] = serviceIdentityId.ToString(),
                    ["serviceName"] = serviceName,
                    ["serviceType"] = serviceType,
                    ["cluster"] = cluster,
                    ["permissions"] = string.Join(",", permissions),
                    ["createdBy"] = createdBy,
                    ["createdAt"] = createdAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.identity.events"
                })
        };

        // 7. Return ServiceIdentityResult
        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["serviceIdentityId"] = serviceIdentityId.ToString(),
                ["serviceName"] = serviceName,
                ["serviceType"] = serviceType,
                ["apiKey"] = apiKey,
                ["createdAt"] = createdAt.ToString("O"),
                ["active"] = true
            }));
    }

    private static Task<EngineResult> RevokeServiceIdentity(EngineContext context)
    {
        // 1. Validate command input
        var serviceIdentityId = context.Data.GetValueOrDefault("serviceIdentityId") as string;
        if (string.IsNullOrEmpty(serviceIdentityId) || !Guid.TryParse(serviceIdentityId, out var identityGuid) || identityGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid serviceIdentityId"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason"));

        // 2. Check identity exists and is active
        var identityActive = context.Data.GetValueOrDefault("identityActive");
        if (identityActive is false or "false")
            return Task.FromResult(EngineResult.Fail("Service identity is not active or does not exist"));

        // 3. Emit ServiceIdentityRevokedEvent
        var revokedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("ServiceIdentityRevoked", identityGuid,
                new Dictionary<string, object>
                {
                    ["serviceIdentityId"] = serviceIdentityId,
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
                ["serviceIdentityId"] = serviceIdentityId,
                ["active"] = false,
                ["reason"] = reason,
                ["revokedAt"] = revokedAt.ToString("O")
            }));
    }

    private static string GenerateApiKey(Guid serviceIdentityId, string serviceName)
    {
        var keyBytes = new byte[32];
        var idBytes = serviceIdentityId.ToByteArray();
        var nameHash = serviceName.GetHashCode();
        var nameBytes = BitConverter.GetBytes(nameHash);

        for (int i = 0; i < keyBytes.Length; i++)
        {
            keyBytes[i] = (byte)(idBytes[i % idBytes.Length] ^ nameBytes[i % nameBytes.Length] ^ (byte)(i * 31));
        }

        return $"wsk_{Convert.ToBase64String(keyBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }

    private static List<string> ResolvePermissions(object? value)
    {
        return value switch
        {
            List<string> list => list,
            string s when !string.IsNullOrEmpty(s) => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            IEnumerable<object> enumerable => enumerable.Select(x => x.ToString() ?? "").Where(x => x.Length > 0).ToList(),
            _ => new List<string>()
        };
    }
}
