namespace Whycespace.ClusterTemplatePlatform;

public sealed class SubClusterTemplate
{
    public string SubClusterName { get; }

    public IReadOnlyCollection<string> DefaultProviders { get; }

    public SubClusterTemplate(string subClusterName, IReadOnlyCollection<string> providers)
    {
        SubClusterName = subClusterName;
        DefaultProviders = providers;
    }
}
