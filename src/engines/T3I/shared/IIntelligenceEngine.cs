namespace Whycespace.Engines.T3I.Shared;

/// <summary>
/// Core contract for all T3I intelligence engines.
/// Engines must be stateless, deterministic, and thread-safe.
/// </summary>
public interface IIntelligenceEngine<TInput, TOutput>
{
    string EngineName { get; }
    IntelligenceResult<TOutput> Execute(IntelligenceContext<TInput> context);
}