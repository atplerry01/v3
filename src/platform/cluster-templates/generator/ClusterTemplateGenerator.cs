namespace Whycespace.ClusterTemplatePlatform;

using Whycespace.ClusterDomain;

public sealed class ClusterTemplateGenerator
{
    private readonly ClusterTemplateRegistry _registry;
    private readonly ClusterAdministrationService _administration;
    private readonly ClusterProviderRegistry _providerRegistry;
    private readonly ProviderAssignmentService _assignmentService;

    public ClusterTemplateGenerator(
        ClusterTemplateRegistry registry,
        ClusterAdministrationService administration,
        ClusterProviderRegistry providerRegistry,
        ProviderAssignmentService assignmentService)
    {
        _registry = registry;
        _administration = administration;
        _providerRegistry = providerRegistry;
        _assignmentService = assignmentService;
    }

    public ClusterGenerationResult GenerateCluster(string templateName)
    {
        var template = _registry.GetTemplate(templateName);

        var cluster = _administration.RegisterCluster(template.ClusterName);

        var subClusterNames = new List<string>();

        foreach (var subTemplate in template.SubClusters)
        {
            _administration.AddSubCluster(cluster.ClusterId, subTemplate.SubClusterName);
            subClusterNames.Add(subTemplate.SubClusterName);

            foreach (var providerName in subTemplate.DefaultProviders)
            {
                var provider = _providerRegistry.RegisterProvider(providerName, providerName, cluster.ClusterId);
                _assignmentService.AssignProviderToSubCluster(provider.ProviderId, subTemplate.SubClusterName);
            }
        }

        foreach (var providerName in template.DefaultProviders)
        {
            var existing = _providerRegistry.GetProvidersByCluster(cluster.ClusterId)
                .Any(p => p.ProviderName == providerName);

            if (!existing)
            {
                _providerRegistry.RegisterProvider(providerName, providerName, cluster.ClusterId);
            }
        }

        return new ClusterGenerationResult(template.ClusterName, subClusterNames.AsReadOnly());
    }
}

public sealed class ClusterGenerationResult
{
    public string Cluster { get; }
    public IReadOnlyCollection<string> SubClusters { get; }

    public ClusterGenerationResult(string cluster, IReadOnlyCollection<string> subClusters)
    {
        Cluster = cluster;
        SubClusters = subClusters;
    }
}
