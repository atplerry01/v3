using Whycespace.Runtime.Observability.Tracing;
using Whycespace.Runtime.Observability.Tracing.Context;

namespace Whycespace.Observability.Tests;

public class TraceManagerTests
{
    [Fact]
    public void StartTrace_CreatesTraceWithRootSpan()
    {
        var manager = new TraceManager();

        var root = manager.StartTrace();

        Assert.NotEqual(Guid.Empty, root.TraceId);
        Assert.NotEqual(Guid.Empty, root.SpanId);
        Assert.Null(root.ParentSpanId);
        Assert.Equal(1, manager.GetActiveTraceCount());
    }

    [Fact]
    public void CreateSpan_AddsChildSpan()
    {
        var manager = new TraceManager();
        var root = manager.StartTrace();

        var child = manager.CreateSpan(root.TraceId, root.SpanId);

        Assert.Equal(root.TraceId, child.TraceId);
        Assert.Equal(root.SpanId, child.ParentSpanId);
        Assert.NotEqual(root.SpanId, child.SpanId);

        var spans = manager.GetSpans(root.TraceId);
        Assert.Equal(2, spans.Count);
    }

    [Fact]
    public void GetSpans_ReturnsEmpty_WhenTraceNotExists()
    {
        var manager = new TraceManager();

        var spans = manager.GetSpans(Guid.NewGuid());

        Assert.Empty(spans);
    }

    [Fact]
    public void TraceId_PropagatesAcrossSpans()
    {
        var manager = new TraceManager();
        var root = manager.StartTrace();

        var span1 = manager.CreateSpan(root.TraceId, root.SpanId);
        var span2 = manager.CreateSpan(root.TraceId, span1.SpanId);

        Assert.Equal(root.TraceId, span1.TraceId);
        Assert.Equal(root.TraceId, span2.TraceId);
        Assert.Equal(span1.SpanId, span2.ParentSpanId);
    }
}
