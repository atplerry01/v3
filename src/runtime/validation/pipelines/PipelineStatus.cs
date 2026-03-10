namespace Whycespace.RuntimeValidation.Pipelines;

public sealed record PipelineStatus(
    string Api,
    string Commands,
    string Workflows,
    string Engines,
    string Events,
    string Projections
)
{
    public static PipelineStatus Healthy() => new("ok", "ok", "ok", "ok", "ok", "ok");

    public static PipelineStatus WithFailure(string component) => component switch
    {
        "api" => new("fail", "ok", "ok", "ok", "ok", "ok"),
        "commands" => new("ok", "fail", "ok", "ok", "ok", "ok"),
        "workflows" => new("ok", "ok", "fail", "ok", "ok", "ok"),
        "engines" => new("ok", "ok", "ok", "fail", "ok", "ok"),
        "events" => new("ok", "ok", "ok", "ok", "fail", "ok"),
        "projections" => new("ok", "ok", "ok", "ok", "ok", "fail"),
        _ => Healthy()
    };
}
