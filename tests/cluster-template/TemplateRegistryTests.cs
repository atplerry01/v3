namespace Whycespace.ClusterTemplatePlatform.Tests;

public sealed class TemplateRegistryTests
{
    [Fact]
    public void ListTemplates_ReturnsRegisteredTemplateNames()
    {
        var registry = new ClusterTemplateRegistry();
        registry.RegisterTemplate(new ClusterTemplate(
            "MobilityTemplate", "WhyceMobility",
            new[] { new SubClusterTemplate("Taxi", new[] { "DriverProvider" }) },
            new[] { "DriverProvider" }));
        registry.RegisterTemplate(new ClusterTemplate(
            "PropertyTemplate", "WhyceProperty",
            new[] { new SubClusterTemplate("LettingAgent", new[] { "PropertyManagerProvider" }) },
            new[] { "PropertyManagerProvider" }));

        var templates = registry.ListTemplates();

        Assert.Contains("MobilityTemplate", templates);
        Assert.Contains("PropertyTemplate", templates);
        Assert.Equal(2, templates.Count);
    }

    [Fact]
    public void GetTemplate_UnknownName_Throws()
    {
        var registry = new ClusterTemplateRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.GetTemplate("NonExistent"));
    }
}
