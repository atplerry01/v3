namespace Whycespace.Shared.Contracts;

public interface IEngine
{
    string Name { get; }
    Task<EngineResult> ExecuteAsync(EngineContext context);
}
