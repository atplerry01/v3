namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record RegisterServiceIdentityCommand(
    string ServiceName,
    string ServiceType,
    string Cluster,
    List<string> Permissions,
    string CreatedBy,
    DateTime Timestamp);
