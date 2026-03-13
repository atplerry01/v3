namespace Whycespace.ClusterTemplatePlatform.Tests;

public sealed class ClusterTemplateTests
{
    [Fact]
    public void RegisterClusterTemplate_IsRetrievable()
    {
        var registry = new ClusterTemplateRegistry();
        var template = new ClusterTemplate(
            "MobilityTemplate",
            "WhyceMobility",
            new[] { new SubClusterTemplate("Taxi", new[] { "DriverProvider" }) },
            new[] { "DriverProvider" });

        registry.RegisterTemplate(template);

        var retrieved = registry.GetTemplate("MobilityTemplate");
        Assert.Equal("MobilityTemplate", retrieved.TemplateName);
        Assert.Equal("WhyceMobility", retrieved.ClusterName);
    }
}
