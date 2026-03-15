namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record RegisterServiceIdentityCommand(
    string ServiceName,
    string ServiceType,
    string Cluster,
    List<string> Permissions,
    string CreatedBy,
    DateTime Timestamp);
