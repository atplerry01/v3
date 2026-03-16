namespace Whycespace.Platform.Dispatch.Handlers;

using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityCommandHandler
{
    private readonly IdentityRegistry _identityRegistry;
    private readonly IdentityAttributeStore _identityAttributeStore;
    private readonly IdentityRoleStore _identityRoleStore;
    private readonly IdentityPermissionStore _identityPermissionStore;
    private readonly IdentityAccessScopeStore _identityAccessScopeStore;
    private readonly IdentityTrustStore _identityTrustStore;
    private readonly IdentityDeviceStore _identityDeviceStore;
    private readonly IdentitySessionStore _identitySessionStore;
    private readonly IdentityConsentStore _identityConsentStore;
    private readonly IdentityGraphStore _identityGraphStore;
    private readonly IdentityServiceStore _identityServiceStore;
    private readonly IdentityFederationStore _identityFederationStore;
    private readonly IdentityRecoveryStore _identityRecoveryStore;
    private readonly IdentityRevocationStore _identityRevocationStore;
    private readonly IdentityAuditStore _identityAuditStore;

    public IdentityCommandHandler(
        IdentityRegistry identityRegistry,
        IdentityAttributeStore identityAttributeStore,
        IdentityRoleStore identityRoleStore,
        IdentityPermissionStore identityPermissionStore,
        IdentityAccessScopeStore identityAccessScopeStore,
        IdentityTrustStore identityTrustStore,
        IdentityDeviceStore identityDeviceStore,
        IdentitySessionStore identitySessionStore,
        IdentityConsentStore identityConsentStore,
        IdentityGraphStore identityGraphStore,
        IdentityServiceStore identityServiceStore,
        IdentityFederationStore identityFederationStore,
        IdentityRecoveryStore identityRecoveryStore,
        IdentityRevocationStore identityRevocationStore,
        IdentityAuditStore identityAuditStore)
    {
        _identityRegistry = identityRegistry;
        _identityAttributeStore = identityAttributeStore;
        _identityRoleStore = identityRoleStore;
        _identityPermissionStore = identityPermissionStore;
        _identityAccessScopeStore = identityAccessScopeStore;
        _identityTrustStore = identityTrustStore;
        _identityDeviceStore = identityDeviceStore;
        _identitySessionStore = identitySessionStore;
        _identityConsentStore = identityConsentStore;
        _identityGraphStore = identityGraphStore;
        _identityServiceStore = identityServiceStore;
        _identityFederationStore = identityFederationStore;
        _identityRecoveryStore = identityRecoveryStore;
        _identityRevocationStore = identityRevocationStore;
        _identityAuditStore = identityAuditStore;
    }

    public bool CanHandle(string command)
    {
        return command.StartsWith("identity.");
    }

    public Task<DispatchResult> HandleAsync(
        string command,
        Dictionary<string, object> payload)
    {
        return command switch
        {
            "identity.trustscore.calculate" => HandleTrustScoreCalculate(payload),
            "identity.authenticate" => HandleAuthenticate(payload),
            "identity.authorize" => HandleAuthorize(payload),
            "identity.consent.grant" => HandleConsentGrant(payload),
            "identity.graph.getRelationships" => HandleGraphGetRelationships(payload),
            "identity.graph.createRelationship" => HandleGraphCreateRelationship(payload),
            "identity.service.getServices" => HandleServiceGetServices(payload),
            "identity.service.register" => HandleServiceRegister(payload),
            "identity.federation.getFederations" => HandleFederationGetFederations(payload),
            "identity.federation.register" => HandleFederationRegister(payload),
            "identity.recovery.getRecoveries" => HandleRecoveryGetRecoveries(payload),
            "identity.recovery.create" => HandleRecoveryCreate(payload),
            "identity.recovery.approve" => HandleRecoveryApprove(payload),
            "identity.recovery.reject" => HandleRecoveryReject(payload),
            "identity.recovery.complete" => HandleRecoveryComplete(payload),
            "identity.revocation.getRevocations" => HandleRevocationGetRevocations(payload),
            "identity.revocation.revoke" => HandleRevocationRevoke(payload),
            "identity.revocation.getAll" => HandleRevocationGetAll(payload),
            "identity.policy.evaluate" => HandlePolicyEvaluate(payload),
            "identity.audit.get" => HandleAuditGet(payload),
            "identity.audit.record" => HandleAuditRecord(payload),
            "identity.audit.getAll" => HandleAuditGetAll(payload),
            _ => Task.FromResult(DispatchResult.Fail($"Unknown identity command: {command}"))
        };
    }

    private Task<DispatchResult> HandleTrustScoreCalculate(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var engine = new TrustScoreEngine(
            _identityRegistry,
            _identityTrustStore);

        var result = engine.Calculate(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["score"] = result.Score,
            ["calculatedAt"] = result.CalculatedAt
        }));
    }

    private Task<DispatchResult> HandleAuthenticate(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var deviceId = (Guid)payload["deviceId"];

        var trustEngine = new TrustScoreEngine(
            _identityRegistry,
            _identityTrustStore);

        var deviceEngine = new DeviceTrustEngine(
            _identityRegistry,
            _identityDeviceStore);

        var engine = new AuthenticationEngine(
            _identityRegistry,
            trustEngine,
            deviceEngine);

        var result = engine.Authenticate(identityId, deviceId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["success"] = result.Success,
            ["message"] = result.Message
        }));
    }

    private Task<DispatchResult> HandleAuthorize(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var resource = (string)payload["resource"];
        var action = (string)payload["action"];
        var scope = (string)payload["scope"];

        var roleEngine = new IdentityRoleEngine(
            _identityRegistry,
            _identityRoleStore);

        var permissionEngine = new IdentityPermissionEngine(
            _identityPermissionStore);

        var scopeEngine = new IdentityAccessScopeEngine(
            _identityAccessScopeStore);

        var engine = new AuthorizationEngine(
            _identityRegistry,
            roleEngine,
            permissionEngine,
            scopeEngine);

        var request = new AuthorizationRequest(
            identityId,
            resource,
            action,
            scope);

        var result = engine.Authorize(request);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["allowed"] = result.Allowed,
            ["reason"] = result.Reason
        }));
    }

    private Task<DispatchResult> HandleConsentGrant(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var target = (string)payload["target"];
        var scope = (string)payload["scope"];

        var engine = new ConsentEngine(
            _identityRegistry,
            _identityConsentStore);

        var result = engine.GrantConsent(identityId, target, scope);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["consentId"] = result.ConsentId,
            ["identityId"] = result.IdentityId,
            ["target"] = result.Target,
            ["scope"] = result.Scope,
            ["grantedAt"] = result.GrantedAt
        }));
    }

    private Task<DispatchResult> HandleGraphGetRelationships(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var engine = new IdentityGraphEngine(
            _identityRegistry,
            _identityGraphStore);

        var relationships = engine.GetRelationships(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["relationships"] = relationships.Select(r => new Dictionary<string, object>
            {
                ["edgeId"] = r.EdgeId,
                ["targetEntityId"] = r.TargetEntityId,
                ["relationship"] = r.Relationship,
                ["createdAt"] = r.CreatedAt
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleGraphCreateRelationship(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var targetEntityId = (Guid)payload["targetEntityId"];
        var relationship = (string)payload["relationship"];

        var engine = new IdentityGraphEngine(
            _identityRegistry,
            _identityGraphStore);

        var result = engine.CreateRelationship(identityId, targetEntityId, relationship);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["edgeId"] = result.EdgeId,
            ["sourceIdentityId"] = result.SourceIdentityId,
            ["targetEntityId"] = result.TargetEntityId,
            ["relationship"] = result.Relationship,
            ["createdAt"] = result.CreatedAt
        }));
    }

    private Task<DispatchResult> HandleServiceGetServices(
        Dictionary<string, object> payload)
    {
        var engine = new ServiceIdentityEngine(_identityServiceStore);

        var services = engine.GetServices();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["services"] = services.Select(s => new Dictionary<string, object>
            {
                ["serviceId"] = s.ServiceId,
                ["name"] = s.Name,
                ["type"] = s.Type,
                ["createdAt"] = s.CreatedAt,
                ["revoked"] = s.Revoked
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleServiceRegister(
        Dictionary<string, object> payload)
    {
        var name = (string)payload["name"];
        var type = (string)payload["type"];
        var secret = (string)payload["secret"];

        var engine = new ServiceIdentityEngine(_identityServiceStore);

        var result = engine.RegisterService(name, type, secret);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["serviceId"] = result.ServiceId,
            ["name"] = result.Name,
            ["type"] = result.Type,
            ["createdAt"] = result.CreatedAt
        }));
    }

    private Task<DispatchResult> HandleFederationGetFederations(
        Dictionary<string, object> payload)
    {
        var engine = new FederationEngine(
            _identityRegistry,
            _identityFederationStore);

        var federations = engine.GetFederations();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["federations"] = federations.Select(f => new Dictionary<string, object>
            {
                ["federationId"] = f.FederationId,
                ["provider"] = f.Provider,
                ["externalIdentityId"] = f.ExternalIdentityId,
                ["internalIdentityId"] = f.InternalIdentityId,
                ["createdAt"] = f.CreatedAt,
                ["revoked"] = f.Revoked
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleFederationRegister(
        Dictionary<string, object> payload)
    {
        var provider = (string)payload["provider"];
        var externalIdentityId = (string)payload["externalIdentityId"];
        var internalIdentityId = (Guid)payload["internalIdentityId"];

        var engine = new FederationEngine(
            _identityRegistry,
            _identityFederationStore);

        var result = engine.RegisterFederation(provider, externalIdentityId, internalIdentityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["federationId"] = result.FederationId,
            ["provider"] = result.Provider,
            ["externalIdentityId"] = result.ExternalIdentityId,
            ["internalIdentityId"] = result.InternalIdentityId,
            ["createdAt"] = result.CreatedAt
        }));
    }

    private Task<DispatchResult> HandleRecoveryGetRecoveries(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var engine = new IdentityRecoveryEngine(
            _identityRegistry,
            _identityRecoveryStore);

        var recoveries = engine.GetRecoveries(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["recoveries"] = recoveries.Select(r => new Dictionary<string, object>
            {
                ["recoveryId"] = r.RecoveryId,
                ["reason"] = r.Reason,
                ["status"] = r.Status,
                ["createdAt"] = r.CreatedAt,
                ["completedAt"] = (object?)r.CompletedAt ?? ""
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleRecoveryCreate(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var reason = (string)payload["reason"];

        var engine = new IdentityRecoveryEngine(
            _identityRegistry,
            _identityRecoveryStore);

        var result = engine.CreateRecovery(identityId, reason);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["recoveryId"] = result.RecoveryId,
            ["identityId"] = result.IdentityId,
            ["reason"] = result.Reason,
            ["status"] = result.Status,
            ["createdAt"] = result.CreatedAt
        }));
    }

    private Task<DispatchResult> HandleRecoveryApprove(
        Dictionary<string, object> payload)
    {
        var recoveryId = (Guid)payload["recoveryId"];

        var engine = new IdentityRecoveryEngine(
            _identityRegistry,
            _identityRecoveryStore);

        engine.ApproveRecovery(recoveryId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["message"] = "Recovery approved",
            ["recoveryId"] = recoveryId
        }));
    }

    private Task<DispatchResult> HandleRecoveryReject(
        Dictionary<string, object> payload)
    {
        var recoveryId = (Guid)payload["recoveryId"];

        var engine = new IdentityRecoveryEngine(
            _identityRegistry,
            _identityRecoveryStore);

        engine.RejectRecovery(recoveryId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["message"] = "Recovery rejected",
            ["recoveryId"] = recoveryId
        }));
    }

    private Task<DispatchResult> HandleRecoveryComplete(
        Dictionary<string, object> payload)
    {
        var recoveryId = (Guid)payload["recoveryId"];

        var engine = new IdentityRecoveryEngine(
            _identityRegistry,
            _identityRecoveryStore);

        engine.CompleteRecovery(recoveryId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["message"] = "Recovery completed",
            ["recoveryId"] = recoveryId
        }));
    }

    private Task<DispatchResult> HandleRevocationGetRevocations(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var engine = new IdentityRevocationEngine(
            _identityRegistry,
            _identityRevocationStore);

        var revocations = engine.GetRevocations(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["revocations"] = revocations.Select(r => new Dictionary<string, object>
            {
                ["revocationId"] = r.RevocationId,
                ["reason"] = r.Reason,
                ["createdAt"] = r.CreatedAt,
                ["active"] = r.Active
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleRevocationRevoke(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var reason = (string)payload["reason"];

        var engine = new IdentityRevocationEngine(
            _identityRegistry,
            _identityRevocationStore);

        var result = engine.RevokeIdentity(identityId, reason);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["revocationId"] = result.RevocationId,
            ["identityId"] = result.IdentityId,
            ["reason"] = result.Reason,
            ["createdAt"] = result.CreatedAt,
            ["active"] = result.Active
        }));
    }

    private Task<DispatchResult> HandleRevocationGetAll(
        Dictionary<string, object> payload)
    {
        var engine = new IdentityRevocationEngine(
            _identityRegistry,
            _identityRevocationStore);

        var revocations = engine.GetAllRevocations();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["revocations"] = revocations.Select(r => new Dictionary<string, object>
            {
                ["revocationId"] = r.RevocationId,
                ["identityId"] = r.IdentityId,
                ["reason"] = r.Reason,
                ["createdAt"] = r.CreatedAt,
                ["active"] = r.Active
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandlePolicyEvaluate(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var adapter = new IdentityPolicyEnforcementAdapter(
            _identityRegistry,
            _identityRoleStore,
            _identityTrustStore,
            _identityRevocationStore);

        var context = adapter.BuildContext(identityId);
        var allowed = adapter.EvaluateIdentityAccess(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["allowed"] = allowed,
            ["context"] = new Dictionary<string, object>
            {
                ["roles"] = context.Roles,
                ["trustScore"] = context.TrustScore,
                ["verified"] = context.IdentityStatus == IdentityStatus.Verified,
                ["revoked"] = context.IdentityStatus == IdentityStatus.Revoked
            }
        }));
    }

    private Task<DispatchResult> HandleAuditGet(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];

        var engine = new IdentityAuditEngine(
            _identityRegistry,
            _identityAuditStore);

        var events = engine.GetIdentityAudit(identityId);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["events"] = events.Select(e => new Dictionary<string, object>
            {
                ["eventId"] = e.EventId,
                ["eventType"] = e.EventType,
                ["description"] = e.Description,
                ["timestamp"] = e.Timestamp
            }).ToList()
        }));
    }

    private Task<DispatchResult> HandleAuditRecord(
        Dictionary<string, object> payload)
    {
        var identityId = (Guid)payload["identityId"];
        var eventType = (string)payload["eventType"];
        var description = (string)payload["description"];

        var engine = new IdentityAuditEngine(
            _identityRegistry,
            _identityAuditStore);

        var result = engine.RecordEvent(identityId, eventType, description);

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["eventId"] = result.EventId,
            ["identityId"] = result.IdentityId,
            ["eventType"] = result.EventType,
            ["description"] = result.Description,
            ["timestamp"] = result.Timestamp
        }));
    }

    private Task<DispatchResult> HandleAuditGetAll(
        Dictionary<string, object> payload)
    {
        var engine = new IdentityAuditEngine(
            _identityRegistry,
            _identityAuditStore);

        var events = engine.GetAllAuditEvents();

        return Task.FromResult(DispatchResult.Ok(new Dictionary<string, object>
        {
            ["events"] = events.Select(e => new Dictionary<string, object>
            {
                ["eventId"] = e.EventId,
                ["identityId"] = e.IdentityId,
                ["eventType"] = e.EventType,
                ["description"] = e.Description,
                ["timestamp"] = e.Timestamp
            }).ToList()
        }));
    }
}
