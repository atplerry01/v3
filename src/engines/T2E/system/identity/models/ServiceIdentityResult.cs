namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record ServiceIdentityResult(
    Guid ServiceIdentityId,
    string ServiceName,
    string ServiceType,
    string ApiKey,
    DateTime CreatedAt,
    bool Active);
