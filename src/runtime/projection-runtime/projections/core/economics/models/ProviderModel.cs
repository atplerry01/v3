namespace Whycespace.ProjectionRuntime.Projections.Core.Economics.Models;

public sealed record ProviderModel(
    Guid ProviderId,
    string ProviderName,
    string ProviderType,
    Guid ClusterId
);
