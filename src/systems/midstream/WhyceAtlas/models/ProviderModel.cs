namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections.Models;

public sealed record ProviderModel(
    Guid ProviderId,
    string ProviderName,
    string ProviderType,
    Guid ClusterId
);
