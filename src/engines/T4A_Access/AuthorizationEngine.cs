namespace Whycespace.Engines.T4A_Access;

using Whycespace.Shared.Contracts;

public sealed class AuthorizationEngine : IEngine
{
    public string Name => "Authorization";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var userId = context.Data.GetValueOrDefault("userId") as string;
        var resource = context.Data.GetValueOrDefault("resource") as string;
        var action = context.Data.GetValueOrDefault("action") as string;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(resource))
            return Task.FromResult(EngineResult.Fail("Missing userId or resource"));

        var authorized = !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(resource);

        if (!authorized)
            return Task.FromResult(EngineResult.Fail($"User {userId} not authorized for {action} on {resource}"));

        var events = new[]
        {
            EngineEvent.Create("UserAuthorized", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["userId"] = userId, ["resource"] = resource, ["action"] = action ?? "read" })
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
