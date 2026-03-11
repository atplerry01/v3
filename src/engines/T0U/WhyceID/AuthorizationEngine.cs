namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

public sealed class AuthorizationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleEngine _roleEngine;
    private readonly IdentityPermissionEngine _permissionEngine;
    private readonly IdentityAccessScopeEngine _scopeEngine;

    public AuthorizationEngine(
        IdentityRegistry registry,
        IdentityRoleEngine roleEngine,
        IdentityPermissionEngine permissionEngine,
        IdentityAccessScopeEngine scopeEngine)
    {
        _registry = registry;
        _roleEngine = roleEngine;
        _permissionEngine = permissionEngine;
        _scopeEngine = scopeEngine;
    }

    public AuthorizationResult Authorize(AuthorizationRequest request)
    {
        if (!_registry.Exists(request.IdentityId))
        {
            return new AuthorizationResult(false, "Identity does not exist");
        }

        var roles = _roleEngine.GetRoles(request.IdentityId);

        var permission = $"{request.Resource}:{request.Action}";

        foreach (var role in roles)
        {
            var hasPermission = _permissionEngine.HasPermission(role, permission);

            if (!hasPermission)
                continue;

            var hasScope = _scopeEngine.HasScope(role, request.Scope);

            if (!hasScope)
                continue;

            return new AuthorizationResult(true, "Authorized");
        }

        return new AuthorizationResult(false, "Permission denied");
    }
}
