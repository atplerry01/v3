namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.EventFabricRuntime.Routing;

public class EventTopicRouterTests
{
    [Fact]
    public void Register_And_ResolveTopic()
    {
        var router = new EventTopicRouter();
        router.Register("CapitalContributionRecorded", "whyce.capital.events");

        var topic = router.ResolveTopic("CapitalContributionRecorded");
        Assert.Equal("whyce.capital.events", topic);
    }

    [Fact]
    public void ResolveTopic_UnregisteredEvent_Throws()
    {
        var router = new EventTopicRouter();

        var ex = Assert.Throws<InvalidOperationException>(() => router.ResolveTopic("Unknown"));
        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void Register_OverwritesExistingRoute()
    {
        var router = new EventTopicRouter();
        router.Register("TestEvent", "topic-a");
        router.Register("TestEvent", "topic-b");

        Assert.Equal("topic-b", router.ResolveTopic("TestEvent"));
    }

    [Fact]
    public void GetRoutes_ReturnsAllRegisteredRoutes()
    {
        var router = new EventTopicRouter();
        router.Register("EventA", "topic-1");
        router.Register("EventB", "topic-2");

        var routes = router.GetRoutes();
        Assert.Equal(2, routes.Count);
        Assert.Equal("topic-1", routes["EventA"]);
        Assert.Equal("topic-2", routes["EventB"]);
    }
}
