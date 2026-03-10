namespace Whycespace.ClusterTemplatePlatform;

public sealed class ClusterTemplate
{
    public string TemplateName { get; }

    public string ClusterName { get; }

    public IReadOnlyCollection<SubClusterTemplate> SubClusters { get; }

    public IReadOnlyCollection<string> DefaultProviders { get; }

    public ClusterTemplate(
        string templateName,
        string clusterName,
        IReadOnlyCollection<SubClusterTemplate> subClusters,
        IReadOnlyCollection<string> providers)
    {
        TemplateName = templateName;
        ClusterName = clusterName;
        SubClusters = subClusters;
        DefaultProviders = providers;
    }
}
