namespace Whycespace.Engines.T4A.Interface.Integration;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Integration", EngineTier.T4A, EngineKind.Mutation, "IntegrationRequest", typeof(EngineEvent))]
public sealed class IntegrationEngine : IEngine
{
    public string Name => "Integration";

    private static readonly IReadOnlyDictionary<string, IntegrationSpec> Integrations =
        new Dictionary<string, IntegrationSpec>
        {
            ["office365"] = new("Office365", "Microsoft Office 365",
                new[] { "calendar.sync", "email.send", "teams.notify", "sharepoint.upload" }),

            ["payments"] = new("Payments", "Payment Gateway",
                new[] { "payment.initiate", "payment.capture", "payment.refund", "payment.status" }),

            ["stripe"] = new("Stripe", "Stripe Payment Processing",
                new[] { "charge.create", "charge.refund", "customer.create", "subscription.manage" }),

            ["sms"] = new("SMS", "SMS Notification Service",
                new[] { "sms.send", "sms.bulk", "sms.status" }),

            ["maps"] = new("Maps", "Geolocation & Mapping",
                new[] { "geocode.forward", "geocode.reverse", "route.calculate", "eta.estimate" }),

            ["kyc"] = new("KYC", "Know Your Customer Verification",
                new[] { "identity.verify", "document.check", "address.verify", "sanctions.screen" }),

            ["banking"] = new("Banking", "Open Banking Integration",
                new[] { "account.verify", "transfer.initiate", "balance.check", "statement.fetch" })
        };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var provider = context.Data.GetValueOrDefault("provider") as string;
        if (string.IsNullOrEmpty(provider))
            return Task.FromResult(EngineResult.Fail("Missing provider"));

        var action = context.Data.GetValueOrDefault("integrationAction") as string;
        if (string.IsNullOrEmpty(action))
            return Task.FromResult(EngineResult.Fail("Missing integrationAction"));

        var callerId = context.Data.GetValueOrDefault("callerId") as string ?? "system";

        if (!Integrations.TryGetValue(provider.ToLowerInvariant(), out var spec))
            return Task.FromResult(EngineResult.Fail(
                $"Unknown provider: {provider}. Available: {string.Join(", ", Integrations.Keys)}"));

        if (!Array.Exists(spec.Actions, a => a == action))
            return Task.FromResult(EngineResult.Fail(
                $"Unknown action '{action}' for provider '{provider}'. Available: {string.Join(", ", spec.Actions)}"));

        return provider.ToLowerInvariant() switch
        {
            "office365" => HandleOffice365(action, callerId, context),
            "payments" or "stripe" => HandlePayment(provider, action, callerId, context),
            "sms" => HandleSms(action, callerId, context),
            "maps" => HandleMaps(action, callerId, context),
            "kyc" => HandleKyc(action, callerId, context),
            "banking" => HandleBanking(action, callerId, context),
            _ => HandleGenericIntegration(provider, action, callerId, context)
        };
    }

    private static Task<EngineResult> HandleOffice365(string action, string callerId, EngineContext context)
    {
        var target = context.Data.GetValueOrDefault("target") as string ?? "";
        var subject = context.Data.GetValueOrDefault("subject") as string ?? "";
        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationOffice365Requested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = "office365",
                    ["action"] = action,
                    ["target"] = target,
                    ["subject"] = subject,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = "office365",
                ["action"] = action,
                ["commandType"] = $"Office365.{ActionVerb(action)}",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandlePayment(string provider, string action, string callerId, EngineContext context)
    {
        var amount = context.Data.GetValueOrDefault("amount");
        var currency = context.Data.GetValueOrDefault("currency") as string ?? "GBP";
        var reference = context.Data.GetValueOrDefault("reference") as string ?? "";
        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationPaymentRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = provider,
                    ["action"] = action,
                    ["amount"] = amount ?? 0,
                    ["currency"] = currency,
                    ["reference"] = reference,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = provider,
                ["action"] = action,
                ["commandType"] = $"Payment.{ActionVerb(action)}",
                ["currency"] = currency,
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleSms(string action, string callerId, EngineContext context)
    {
        var recipient = context.Data.GetValueOrDefault("recipient") as string;
        if (string.IsNullOrEmpty(recipient))
            return Task.FromResult(EngineResult.Fail("Missing recipient for SMS action"));

        var message = context.Data.GetValueOrDefault("message") as string ?? "";
        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationSmsRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = "sms",
                    ["action"] = action,
                    ["recipient"] = recipient,
                    ["messageLength"] = message.Length,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = "sms",
                ["action"] = action,
                ["commandType"] = $"Sms.{ActionVerb(action)}",
                ["recipient"] = recipient,
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleMaps(string action, string callerId, EngineContext context)
    {
        var latitude = context.Data.GetValueOrDefault("latitude");
        var longitude = context.Data.GetValueOrDefault("longitude");
        var address = context.Data.GetValueOrDefault("address") as string;
        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationMapsRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = "maps",
                    ["action"] = action,
                    ["hasCoordinates"] = latitude is not null && longitude is not null,
                    ["hasAddress"] = !string.IsNullOrEmpty(address),
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = "maps",
                ["action"] = action,
                ["commandType"] = $"Maps.{ActionVerb(action)}",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleKyc(string action, string callerId, EngineContext context)
    {
        var subjectId = context.Data.GetValueOrDefault("subjectId") as string;
        if (string.IsNullOrEmpty(subjectId))
            return Task.FromResult(EngineResult.Fail("Missing subjectId for KYC action"));

        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationKycRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = "kyc",
                    ["action"] = action,
                    ["subjectId"] = subjectId,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = "kyc",
                ["action"] = action,
                ["subjectId"] = subjectId,
                ["commandType"] = $"Kyc.{ActionVerb(action)}",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleBanking(string action, string callerId, EngineContext context)
    {
        var accountRef = context.Data.GetValueOrDefault("accountRef") as string;
        if (string.IsNullOrEmpty(accountRef))
            return Task.FromResult(EngineResult.Fail("Missing accountRef for banking action"));

        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationBankingRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = "banking",
                    ["action"] = action,
                    ["accountRef"] = accountRef,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = "banking",
                ["action"] = action,
                ["accountRef"] = accountRef,
                ["commandType"] = $"Banking.{ActionVerb(action)}",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleGenericIntegration(string provider, string action, string callerId, EngineContext context)
    {
        var integrationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("IntegrationRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["integrationId"] = integrationId.ToString(),
                    ["provider"] = provider,
                    ["action"] = action,
                    ["callerId"] = callerId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["integrationId"] = integrationId.ToString(),
                ["provider"] = provider,
                ["action"] = action,
                ["dispatched"] = true
            }));
    }

    private static string ActionVerb(string action)
    {
        var dotIndex = action.IndexOf('.');
        if (dotIndex < 0 || dotIndex >= action.Length - 1)
            return action;
        var verb = action[(dotIndex + 1)..];
        return char.ToUpperInvariant(verb[0]) + verb[1..];
    }

    private sealed record IntegrationSpec(string Name, string Description, string[] Actions);
}
