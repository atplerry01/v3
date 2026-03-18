namespace Whycespace.Engines.T0U.WhyceID.Authentication;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

public sealed class AuthenticationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly TrustScoreEngine _trustEngine;
    private readonly DeviceTrustEngine _deviceEngine;

    public AuthenticationEngine(
        IdentityRegistry registry,
        TrustScoreEngine trustEngine,
        DeviceTrustEngine deviceEngine)
    {
        _registry = registry;
        _trustEngine = trustEngine;
        _deviceEngine = deviceEngine;
    }

    public AuthenticationResult Authenticate(
        Guid identityId,
        Guid deviceId)
    {
        if (!_registry.Exists(identityId))
        {
            return new AuthenticationResult(
                false,
                "Identity does not exist");
        }

        var identity = _registry.Get(identityId);

        if (identity.Status != IdentityStatus.Verified)
        {
            return new AuthenticationResult(
                false,
                "Identity not verified");
        }

        if (!_deviceEngine.IsTrusted(identityId, deviceId))
        {
            return new AuthenticationResult(
                false,
                "Device not trusted");
        }

        var trust = _trustEngine.Get(identityId);

        if (trust is null || trust.Score < 50)
        {
            return new AuthenticationResult(
                false,
                "Trust score too low");
        }

        return new AuthenticationResult(
            true,
            "Authentication successful");
    }
}
