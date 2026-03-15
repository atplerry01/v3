namespace Whycespace.Engines.T2E.System.Providers;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("ClusterProviderRegistration", EngineTier.T2E, EngineKind.Mutation, "ProviderRegistrationRequest", typeof(EngineEvent))]
public sealed class ClusterProviderRegistrationEngine : IEngine
{
    public string Name => "ClusterProviderRegistration";

    private static readonly string[] SupportedProviderTypes = { "DriverProvider", "PropertyManager", "EnergyOperator" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var providerName = context.Data.GetValueOrDefault("providerName") as string;
        if (string.IsNullOrEmpty(providerName))
            return Task.FromResult(EngineResult.Fail("Missing providerName"));

        var providerType = context.Data.GetValueOrDefault("providerType") as string;
        if (string.IsNullOrEmpty(providerType))
            return Task.FromResult(EngineResult.Fail("Missing providerType"));

        if (!Array.Exists(SupportedProviderTypes, t => t == providerType))
            return Task.FromResult(EngineResult.Fail($"Unsupported providerType: {providerType}. Supported: {string.Join(", ", SupportedProviderTypes)}"));

        var providerId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("ProviderRegistered", providerId,
                new Dictionary<string, object>
                {
                    ["providerId"] = providerId.ToString(),
                    ["providerName"] = providerName,
                    ["providerType"] = providerType,
                    ["topic"] = "whyce.providers.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["providerId"] = providerId.ToString(),
                ["providerName"] = providerName,
                ["providerType"] = providerType
            }));
    }
}
