namespace Whycespace.Contracts.Engines;

public interface IEngine
{
    string Name { get; }
    Task<EngineResult> ExecuteAsync(EngineContext context);
}
