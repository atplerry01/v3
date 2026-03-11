namespace Whycespace.Tests.Guardrails;

using Whycespace.ArchitectureGuardrails.Validation;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Contracts.Engines;

public sealed class EngineValidatorTests
{
    private readonly EngineArchitectureValidator _validator = new();

    [Fact]
    public void RideExecutionEngine_Passes_Validation()
    {
        var result = _validator.ValidateEngine(typeof(RideExecutionEngine));
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void AllEngines_Pass_Validation()
    {
        var assembly = typeof(RideExecutionEngine).Assembly;
        var results = _validator.ValidateAllEngines(assembly);

        Assert.NotEmpty(results);
        foreach (var result in results)
            Assert.True(result.IsValid, $"{result.EngineName}: {string.Join("; ", result.Violations)}");
    }

    [Fact]
    public void NonSealedEngine_Fails_Validation()
    {
        var result = _validator.ValidateEngine(typeof(TestNonSealedEngine));
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("sealed"));
    }

    [Fact]
    public void Engine_WithMutableField_Fails_Validation()
    {
        var result = _validator.ValidateEngine(typeof(TestMutableFieldEngine));
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("mutable instance fields"));
    }
}

// Test fixtures — intentionally violate rules
public class TestNonSealedEngine : IEngine
{
    public string Name => "TestNonSealed";
    public Task<EngineResult> ExecuteAsync(EngineContext context) => Task.FromResult(EngineResult.Fail("test"));
}

public sealed class TestMutableFieldEngine : IEngine
{
    private int _counter;
    public string Name => "TestMutable";
    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        _counter++;
        return Task.FromResult(EngineResult.Fail("test"));
    }
}
