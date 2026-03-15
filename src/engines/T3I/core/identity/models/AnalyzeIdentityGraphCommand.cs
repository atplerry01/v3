namespace Whycespace.Engines.T3I.Core.Identity.Models;

public sealed record AnalyzeIdentityGraphCommand(
    Guid IdentityId,
    List<string> ConnectedDevices,
    List<Guid> ConnectedProviders,
    List<Guid> ConnectedOperators,
    List<Guid> ConnectedServices,
    DateTime Timestamp);
