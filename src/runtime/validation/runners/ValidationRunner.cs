namespace Whycespace.RuntimeValidation.Runners;

using Whycespace.RuntimeValidation.Models;
using Whycespace.RuntimeValidation.Reports;
using Whycespace.RuntimeValidation.Scenarios;

public sealed class ValidationRunner
{
    private readonly List<ValidationReport> _reports = new();
    private readonly TaxiRideScenario _taxiScenario = new();
    private readonly PropertyLettingScenario _propertyScenario = new();

    public IReadOnlyList<ValidationScenario> GetScenarios() =>
    [
        TaxiRideScenario.Definition,
        PropertyLettingScenario.Definition
    ];

    public async Task<IReadOnlyList<ValidationReport>> RunAllAsync()
    {
        _reports.Clear();

        var taxiReport = await _taxiScenario.ExecuteAsync();
        _reports.Add(taxiReport);

        var propertyReport = await _propertyScenario.ExecuteAsync();
        _reports.Add(propertyReport);

        return _reports;
    }

    public async Task<ValidationReport> RunScenarioAsync(Guid scenarioId)
    {
        if (scenarioId == TaxiRideScenario.Definition.ScenarioId)
        {
            var report = await _taxiScenario.ExecuteAsync();
            _reports.Add(report);
            return report;
        }

        if (scenarioId == PropertyLettingScenario.Definition.ScenarioId)
        {
            var report = await _propertyScenario.ExecuteAsync();
            _reports.Add(report);
            return report;
        }

        return new ValidationReport(scenarioId, "Unknown", false, TimeSpan.Zero, [], $"Unknown scenario: {scenarioId}");
    }

    public IReadOnlyList<ValidationReport> GetResults() => _reports;
}
